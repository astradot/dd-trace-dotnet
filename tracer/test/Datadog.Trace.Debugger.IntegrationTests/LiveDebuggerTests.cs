// <copyright file="LiveDebuggerTests.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Datadog.Trace.Configuration;
using Datadog.Trace.Configuration.Telemetry;
using Datadog.Trace.Debugger.IntegrationTests.Assertions;
using Datadog.Trace.Debugger.IntegrationTests.Helpers;
using Datadog.Trace.Debugger.Sink;
using Datadog.Trace.Logging;
using Datadog.Trace.TestHelpers;
using Samples.Probes.TestRuns.SmokeTests;
using VerifyXunit;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.Debugger.IntegrationTests;

#if !NETCOREAPP2_1
[CollectionDefinition(nameof(LiveDebuggerTests), DisableParallelization = true)]
[Collection(nameof(LiveDebuggerTests))]
[UsesVerify]
public class LiveDebuggerTests : TestHelper
{
    private const string LogFileNamePrefix = "dotnet-tracer-managed-";
    private const string LiveDebuggerDisabledLogEntry = "Live Debugger is disabled. To enable it, please set DD_DYNAMIC_INSTRUMENTATION_ENABLED environment variable to 'true'.";

    public LiveDebuggerTests(ITestOutputHelper output)
        : base("Probes", Path.Combine("test", "test-applications", "debugger"), output)
    {
        SetServiceVersion("1.0.0");
    }

    [SkippableFact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "ArmUnsupported")]
    [Trait("RunOnWindows", "True")]
    [Trait("Category", "LinuxUnsupported")]
    public async Task LiveDebuggerDisabled_DebuggerDisabledByDefault_NoDebuggerTypesCreated()
    {
#if NET8_0_OR_GREATER
        // These tests often hang on x86 on .NET 8+. Needs investigation
        Skip.If(!EnvironmentTools.IsTestTarget64BitProcess());
#endif
        await RunTest();
    }

    [SkippableFact]
    [Trait("Category", "EndToEnd")]
    [Trait("Category", "ArmUnsupported")]
    [Trait("RunOnWindows", "True")]
    [Trait("Category", "LinuxUnsupported")]
    public async Task LiveDebuggerDisabled_DebuggerExplicitlyDisabled_NoDebuggerTypesCreated()
    {
#if NET8_0_OR_GREATER
        // These tests often hang on x86 on .NET 8+. Needs investigation
        Skip.If(!EnvironmentTools.IsTestTarget64BitProcess());
#endif
        SetEnvironmentVariable(ConfigurationKeys.Debugger.Enabled, "0");
        await RunTest();
    }

    private async Task RunTest([CallerMemberName] string testName = null)
    {
        // Create a subdirectory for the logs based on the test name and suffix
        // And write logs there instead
        var logPath = Path.Combine(LogDirectory, $"{testName}");
        Directory.CreateDirectory(logPath);
        SetEnvironmentVariable(ConfigurationKeys.LogDirectory, logPath);

        var testType = DebuggerTestHelper.SpecificTestDescription(typeof(AsyncVoid));

        using var agent = EnvironmentHelper.GetMockAgent();
        string processName = EnvironmentHelper.IsCoreClr() ? "dotnet" : "Samples.Probes";
        using var logEntryWatcher = new LogEntryWatcher($"{LogFileNamePrefix}{processName}*", logPath, Output);
        using var sample = await StartSample(agent, $"--test-name {testType.TestType}", string.Empty, aspNetCorePort: 5000);
        await logEntryWatcher.WaitForLogEntry(LiveDebuggerDisabledLogEntry);

        try
        {
            var memoryAssertions = await MemoryAssertions.CaptureSnapshotToAssertOn(sample, Output);

            memoryAssertions.NoObjectsExist<SnapshotSink>();
            memoryAssertions.NoObjectsExist<LineProbeResolver>();
        }
        finally
        {
            if (!sample.HasExited)
            {
                sample.Kill();
            }
        }
    }
}
#endif
