using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Datadog.Core.Tools;
using Datadog.Trace.ExtensionMethods;
using MessagePack;

namespace Datadog.Trace.TestHelpers
{
    public class MockTracerAgent : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _tracesPipeName;
        private readonly Thread _namedPipeThread;
        private readonly UdpClient _udpClient;
        private readonly Thread _httpListenerThread;
        private readonly Thread _statsdThread;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public MockTracerAgent(int port = 8126, int retries = 5, bool useStatsd = false, string tracePipeName = null)
        {
            _tracesPipeName = tracePipeName;
            _cancellationTokenSource = new CancellationTokenSource();

            if (useStatsd)
            {
                const int basePort = 11555;

                var retriesLeft = retries;

                while (true)
                {
                    try
                    {
                        _udpClient = new UdpClient(basePort + retriesLeft);
                    }
                    catch (Exception) when (retriesLeft > 0)
                    {
                        retriesLeft--;
                        continue;
                    }

                    _statsdThread = new Thread(HandleStatsdRequests) { IsBackground = true };
                    _statsdThread.Start();

                    StatsdPort = basePort + retriesLeft;

                    break;
                }
            }

            if (tracePipeName != null)
            {
                _namedPipeThread = new Thread(NamedPipeServerThread) { IsBackground = true };
                _namedPipeThread.Start();
            }
            else
            {
                // try up to 5 consecutive ports before giving up
                while (true)
                {
                    // seems like we can't reuse a listener if it fails to start,
                    // so create a new listener each time we retry
                    var listener = new HttpListener();
                    listener.Prefixes.Add($"http://127.0.0.1:{port}/");
                    listener.Prefixes.Add($"http://localhost:{port}/");

                    try
                    {
                        listener.Start();

                        // successfully listening
                        Port = port;
                        _listener = listener;

                        _httpListenerThread = new Thread(HandleHttpRequests);
                        _httpListenerThread.Start();

                        return;
                    }
                    catch (HttpListenerException) when (retries > 0)
                    {
                        // only catch the exception if there are retries left
                        port = TcpPortProvider.GetOpenPort();
                        retries--;
                    }

                    // always close listener if exception is thrown,
                    // whether it was caught or not
                    listener.Close();
                }
            }
        }

        public event EventHandler<EventArgs<HttpListenerContext>> RequestReceived;

        public event EventHandler<EventArgs<IList<IList<Span>>>> RequestDeserialized;

        /// <summary>
        /// Gets or sets a value indicating whether to skip serialization of traces.
        /// </summary>
        public bool ShouldDeserializeTraces { get; set; } = true;

        /// <summary>
        /// Gets the TCP port that this Agent is listening on.
        /// Can be different from <see cref="MockTracerAgent(int, int)"/>'s <c>initialPort</c>
        /// parameter if listening on that port fails.
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Gets the UDP port for statsd
        /// </summary>
        public int StatsdPort { get; }

        /// <summary>
        /// Gets the filters used to filter out spans we don't want to look at for a test.
        /// </summary>
        public List<Func<Span, bool>> SpanFilters { get; private set; } = new List<Func<Span, bool>>();

        public IImmutableList<Span> Spans { get; private set; } = ImmutableList<Span>.Empty;

        public IImmutableList<NameValueCollection> RequestHeaders { get; private set; } = ImmutableList<NameValueCollection>.Empty;

        public ConcurrentQueue<string> StatsdRequests { get; } = new ConcurrentQueue<string>();

        /// <summary>
        /// Wait for the given number of spans to appear.
        /// </summary>
        /// <param name="count">The expected number of spans.</param>
        /// <param name="timeoutInMilliseconds">The timeout</param>
        /// <param name="operationName">The integration we're testing</param>
        /// <param name="minDateTime">Minimum time to check for spans from</param>
        /// <param name="returnAllOperations">When true, returns every span regardless of operation name</param>
        /// <returns>The list of spans.</returns>
        public IImmutableList<Span> WaitForSpans(
            int count,
            int timeoutInMilliseconds = 20000,
            string operationName = null,
            DateTimeOffset? minDateTime = null,
            bool returnAllOperations = false)
        {
            var deadline = DateTime.Now.AddMilliseconds(timeoutInMilliseconds);
            var minimumOffset = (minDateTime ?? DateTimeOffset.MinValue).ToUnixTimeNanoseconds();

            IImmutableList<Span> relevantSpans = ImmutableList<Span>.Empty;

            while (DateTime.Now < deadline)
            {
                relevantSpans =
                    Spans
                       .Where(s => SpanFilters.All(shouldReturn => shouldReturn(s)))
                       .Where(s => s.Start > minimumOffset)
                       .ToImmutableList();

                if (relevantSpans.Count(s => operationName == null || s.Name == operationName) >= count)
                {
                    break;
                }

                Thread.Sleep(500);
            }

            foreach (var headers in RequestHeaders)
            {
                // This is the place to check against headers we expect
                AssertHeader(
                    headers,
                    "X-Datadog-Trace-Count",
                    header =>
                    {
                        if (int.TryParse(header, out int traceCount))
                        {
                            return traceCount >= 0;
                        }

                        return false;
                    });
            }

            if (!returnAllOperations)
            {
                relevantSpans =
                    relevantSpans
                       .Where(s => operationName == null || s.Name == operationName)
                       .ToImmutableList();
            }

            return relevantSpans;
        }

        public void Dispose()
        {
            _listener?.Stop();
            _cancellationTokenSource.Cancel();
            _udpClient?.Close();
        }

        protected virtual void OnRequestReceived(HttpListenerContext context)
        {
            RequestReceived?.Invoke(this, new EventArgs<HttpListenerContext>(context));
        }

        protected virtual void OnRequestDeserialized(IList<IList<Span>> traces)
        {
            RequestDeserialized?.Invoke(this, new EventArgs<IList<IList<Span>>>(traces));
        }

        private void AssertHeader(
            NameValueCollection headers,
            string headerKey,
            Func<string, bool> assertion)
        {
            var header = headers.Get(headerKey);

            if (string.IsNullOrEmpty(header))
            {
                throw new Exception($"Every submission to the agent should have a {headerKey} header.");
            }

            if (!assertion(header))
            {
                throw new Exception($"Failed assertion for {headerKey} on {header}");
            }
        }

        private void HandleStatsdRequests()
        {
            var endPoint = new IPEndPoint(IPAddress.Loopback, 0);

            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var buffer = _udpClient.Receive(ref endPoint);

                    StatsdRequests.Enqueue(Encoding.UTF8.GetString(buffer));
                }
                catch (Exception) when (_cancellationTokenSource.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        private void HandleHttpRequests()
        {
            while (_listener.IsListening)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    OnRequestReceived(ctx);

                    if (ShouldDeserializeTraces)
                    {
                        var spans = MessagePackSerializer.Deserialize<IList<IList<Span>>>(ctx.Request.InputStream);
                        OnRequestDeserialized(spans);

                        lock (this)
                        {
                            // we only need to lock when replacing the span collection,
                            // not when reading it because it is immutable
                            Spans = Spans.AddRange(spans.SelectMany(trace => trace));
                            RequestHeaders = RequestHeaders.Add(new NameValueCollection(ctx.Request.Headers));
                        }
                    }

                    // NOTE: HttpStreamRequest doesn't support Transfer-Encoding: Chunked
                    // (Setting content-length avoids that)

                    ctx.Response.ContentType = "application/json";
                    var buffer = Encoding.UTF8.GetBytes("{}");
                    ctx.Response.ContentLength64 = buffer.LongLength;
                    ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    ctx.Response.Close();
                }
                catch (HttpListenerException)
                {
                    // listener was stopped,
                    // ignore to let the loop end and the method return
                }
                catch (ObjectDisposedException)
                {
                    // the response has been already disposed.
                }
                catch (Exception) when (!_listener.IsListening)
                {
                    // we don't care about any exception when listener is stopped
                }
            }
        }

        private void NamedPipeServerThread()
        {
            using (var pipeServer = new NamedPipeServerStream(_tracesPipeName, PipeDirection.InOut, 1))
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;

                while (_cancellationTokenSource.IsCancellationRequested == false)
                {
                    // Wait for a client to connect
                    pipeServer.WaitForConnection();

                    Console.WriteLine("Client connected on thread[{0}].", threadId);
                    try
                    {
                        var request = ReadRequest(pipeServer);

                        var response =
                            @"HTTP/1.1 200 OK
ContentType: application/json
Content-Length: 0

";
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        pipeServer.Write(responseBytes, 0, responseBytes.Length);

                        if (request.Body != null && request.Body.Length > 1)
                        {
                            var spans = MessagePackSerializer.Deserialize<IList<IList<Span>>>(request.Body);
                            OnRequestDeserialized(spans);
                        }
                    }
                    catch (IOException e)
                    {
                        Console.WriteLine("ERROR: {0}", e.Message);
                    }
                    finally
                    {
                        pipeServer.Disconnect();
                    }
                }
            }
        }

        private MockRequest ReadRequest(NamedPipeServerStream pipeServer)
        {
            var batchRead = new byte[0x350];
            var headerIndex = 0;
            var headerBuffer = new byte[0x1000];

            // byte nullByte = 0x0;
            var carriageReturn = (byte)'\r';
            var lineFeed = (byte)'\n';

            bool EndOfHeaders()
            {
                if (headerIndex < 3) { return false; }

                if (headerBuffer[headerIndex] != lineFeed) { return false; }

                if (headerBuffer[headerIndex - 1] != carriageReturn) { return false; }

                if (headerBuffer[headerIndex - 2] != lineFeed) { return false; }

                if (headerBuffer[headerIndex - 3] != carriageReturn) { return false; }

                return true;
            }

            string headerString;
            var leftoverIndex = 0;
            var leftoverBytes = new byte[0x350];

            var endOfStream = false;
            do
            {
                pipeServer.Read(batchRead, 0, batchRead.Length);

                foreach (var t in batchRead)
                {
                    if (endOfStream)
                    {
                        leftoverBytes[leftoverIndex++] = t;
                    }
                    else
                    {
                        headerBuffer[headerIndex] = t;
                    }

                    if (EndOfHeaders())
                    {
                        endOfStream = true;
                        continue;
                    }

                    headerIndex++;
                }
            }
            while (!endOfStream);

            Array.Resize(ref headerBuffer, headerIndex + 1);

            headerString = Encoding.UTF8.GetString(headerBuffer);
            var headerLines = headerString.Split(
                new[] { Environment.NewLine },
                StringSplitOptions.None);

            var index = 0;
            var currentLine = headerLines[index];

            var mockRequest = new MockRequest();

            var contentLength = 0;
            while (!string.IsNullOrWhiteSpace(currentLine))
            {
                var semicolonIndex = currentLine.IndexOf(":");
                if (semicolonIndex > -1)
                {
                    var key = currentLine.Substring(0, semicolonIndex);
                    var value = currentLine.Substring(semicolonIndex + 1, currentLine.Length - semicolonIndex - 1);

                    if (key.Equals("content-length", StringComparison.OrdinalIgnoreCase))
                    {
                        contentLength = int.Parse(value);
                    }

                    mockRequest.Headers.Add(new KeyValuePair<string, string>(key, value));
                }

                currentLine = headerLines[++index];
            }

            var body = new byte[contentLength];

            if (contentLength < leftoverIndex)
            {
                Array.Copy(leftoverBytes, body, contentLength);
            }
            else
            {
                var bodyRemainder = contentLength - leftoverIndex;
                Array.Copy(leftoverBytes, body, leftoverIndex);
                pipeServer.Read(body, leftoverIndex, bodyRemainder);
            }

            mockRequest.Body = body;

            return mockRequest;
        }

        [MessagePackObject]
        [DebuggerDisplay("TraceId={TraceId}, SpanId={SpanId}, Service={Service}, Name={Name}, Resource={Resource}")]
        public struct Span
        {
            [Key("trace_id")]
            public ulong TraceId { get; set; }

            [Key("span_id")]
            public ulong SpanId { get; set; }

            [Key("name")]
            public string Name { get; set; }

            [Key("resource")]
            public string Resource { get; set; }

            [Key("service")]
            public string Service { get; set; }

            [Key("type")]
            public string Type { get; set; }

            [Key("start")]
            public long Start { get; set; }

            [Key("duration")]
            public long Duration { get; set; }

            [Key("parent_id")]
            public ulong? ParentId { get; set; }

            [Key("error")]
            public byte Error { get; set; }

            [Key("meta")]
            public Dictionary<string, string> Tags { get; set; }

            [Key("metrics")]
            public Dictionary<string, double> Metrics { get; set; }

            public override string ToString()
            {
                return $"TraceId={TraceId}, SpanId={SpanId}, Service={Service}, Name={Name}, Resource={Resource}, Type={Type}";
            }
        }

        public class MockRequest
        {
            public List<KeyValuePair<string, string>> Headers { get; set; } = new List<KeyValuePair<string, string>>();

            public byte[] Body { get; set; }
        }
    }
}
