using System;
using System.Threading;
using NUnit.Framework;

namespace Samples.NUnitTestsRetries;

public class TestSuite
{
    private static int _retryCount;
    private static int _trueAtLastRetryCount = -1;
    private static int _trueAtThirdRetryCount = -1;

    static TestSuite()
    {
        var strRetryCount = Environment.GetEnvironmentVariable("DD_CIVISIBILITY_FLAKY_RETRY_COUNT");
        int.TryParse(strRetryCount, out _retryCount);
    }

    [Test]
    public void AlwaysPasses()
    {
    }
    
    [Test]
    public void AlwaysFails()
    {
        throw new Exception("This test should always fail");
    }

    [Test]
    public void TrueAtLastRetry()
    {
        if (Interlocked.Increment(ref _trueAtLastRetryCount) != _retryCount)
        {
            throw new Exception("This test should be retried.");
        }
    }
    
    [Test]
    public void TrueAtThirdRetry()
    {
        if (Interlocked.Increment(ref _trueAtThirdRetryCount) != 3)
        {
            throw new Exception("This test should be retried.");
        }
    }
}
