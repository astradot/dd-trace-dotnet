// <copyright file="XUnitIntegration.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Datadog.Trace.Ci;
using Datadog.Trace.Ci.Tags;
using Datadog.Trace.Configuration;

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.XUnit;

internal static class XUnitIntegration
{
    internal const string IntegrationName = nameof(IntegrationId.XUnit);
    internal const IntegrationId IntegrationId = Configuration.IntegrationId.XUnit;

    private static long _totalTestCases;
    private static long _newTestCases;

    internal static bool IsEnabled => TestOptimization.Instance.IsRunning && Tracer.Instance.Settings.IsIntegrationEnabled(IntegrationId);

    internal static Test? CreateTest(ref TestRunnerStruct runnerInstance, TestCaseMetadata? testCaseMetadata = null)
    {
        // Get the test suite instance
        var testSuite = TestSuite.Current;
        if (testSuite is null)
        {
            Common.Log.Warning("Test suite cannot be found.");
            return null;
        }

        var testMethod = runnerInstance.TestMethod;
        if (testMethod is not null)
        {
            // Prepare the method by jit-compiling it before running the test (if possible) and avoid the overhead of the first execution
            RuntimeHelpers.PrepareMethod(testMethod.MethodHandle);
        }

        var test = testSuite.InternalCreateTest(testMethod?.Name ?? string.Empty);

        // Get test parameters
        var testMethodArguments = runnerInstance.TestMethodArguments;
        var methodParameters = testMethod?.GetParameters();
        if (methodParameters?.Length > 0 && testMethodArguments?.Length > 0)
        {
            var testParameters = new TestParameters
            {
                Metadata = new Dictionary<string, object?>(),
                Arguments = new Dictionary<string, object?>()
            };
            testParameters.Metadata[TestTags.MetadataTestName] = runnerInstance.TestCase.DisplayName ?? string.Empty;

            for (var i = 0; i < methodParameters.Length; i++)
            {
                var key = methodParameters[i].Name ?? string.Empty;
                if (i < testMethodArguments.Length)
                {
                    testParameters.Arguments[key] = Common.GetParametersValueData(testMethodArguments[i]);
                }
                else
                {
                    testParameters.Arguments[key] = "(default)";
                }
            }

            test.SetParameters(testParameters);
        }

        // Get traits
        if (runnerInstance.TestCase.Traits is { } traits)
        {
            // Unskippable tests support
            if (TestOptimization.Instance.Settings.IntelligentTestRunnerEnabled)
            {
                ShouldSkip(ref runnerInstance, out var isUnskippable, out var isForcedRun, traits);
                test.SetTag(IntelligentTestRunnerTags.UnskippableTag, isUnskippable ? "true" : "false");
                test.SetTag(IntelligentTestRunnerTags.ForcedRunTag, isForcedRun ? "true" : "false");
                traits.Remove(IntelligentTestRunnerTags.UnskippableTraitName);
            }

            test.SetTraits(traits);
        }
        else if (TestOptimization.Instance.Settings.IntelligentTestRunnerEnabled)
        {
            // Unskippable tests support
            test.SetTag(IntelligentTestRunnerTags.UnskippableTag, "false");
            test.SetTag(IntelligentTestRunnerTags.ForcedRunTag, "false");
        }

        // Early flake detection flags
        if (TestOptimization.Instance.Settings.EarlyFlakeDetectionEnabled == true)
        {
            var testIsNew = !TestOptimization.Instance.EarlyFlakeDetectionFeature?.IsAnEarlyFlakeDetectionTest(test.Suite.Module.Name, test.Suite.Name, test.Name ?? string.Empty) ?? false;
            if (testIsNew)
            {
                test.SetTag(EarlyFlakeDetectionTags.TestIsNew, "true");

                if (testCaseMetadata is null)
                {
                    Interlocked.Increment(ref _newTestCases);
                }
            }

            if (testCaseMetadata is not null)
            {
                testCaseMetadata.TestIsNew = testIsNew;
                if (testIsNew)
                {
                    if (testCaseMetadata.ExecutionIndex > 0)
                    {
                        test.SetTag(EarlyFlakeDetectionTags.TestIsRetry, "true");
                    }
                    else
                    {
                        Interlocked.Increment(ref _newTestCases);
                    }
                }

                Common.CheckFaultyThreshold(test, Interlocked.Read(ref _newTestCases), Interlocked.Read(ref _totalTestCases));
            }
        }

        // Flaky retries
        if (TestOptimization.Instance.Settings.FlakyRetryEnabled == true)
        {
            if (testCaseMetadata is { ExecutionIndex: > 0 })
            {
                test.SetTag(EarlyFlakeDetectionTags.TestIsRetry, "true");
            }
        }

        // Test code and code owners
        if (testMethod is not null)
        {
            test.SetTestMethodInfo(testMethod);
        }

        // Telemetry
        Tracer.Instance.TracerManager.Telemetry.IntegrationGeneratedSpan(IntegrationId);

        // Skip tests
        if (runnerInstance.SkipReason is { } skipReason)
        {
            test.Close(TestStatus.Skip, skipReason: skipReason, duration: TimeSpan.Zero);
            return null;
        }

        test.ResetStartTime();
        return test;
    }

    internal static void FinishTest(Test test, IExceptionAggregator? exceptionAggregator)
    {
        try
        {
            TimeSpan? duration = null;
            var testTags = test.GetTags();
            if (testTags.EarlyFlakeDetectionTestIsNew == "true" && test.GetInternalSpan() is Span internalSpan)
            {
                duration = internalSpan.Context.TraceContext.Clock.ElapsedSince(internalSpan.StartTime);
                if (duration.Value.TotalMinutes >= 5)
                {
                    testTags.EarlyFlakeDetectionTestAbortReason = "slow";
                }
            }

            if (exceptionAggregator?.ToException() is { } exception)
            {
                if (exception.GetType().Name == "SkipException")
                {
                    var skipReason = exception.Message.Replace("$XunitDynamicSkip$", string.Empty);
                    test.Close(TestStatus.Skip, TimeSpan.Zero, skipReason);
                }
                else
                {
                    test.SetErrorInfo(exception);
                    test.Close(TestStatus.Fail, duration);
                }
            }
            else
            {
                test.Close(TestStatus.Pass, duration);
            }
        }
        catch (Exception ex)
        {
            TestOptimization.Instance.Log.Warning(ex, "Error finishing test scope");
            test.Close(TestStatus.Pass);
        }
    }

    internal static bool ShouldSkip(ref TestRunnerStruct runnerInstance, out bool isUnskippable, out bool isForcedRun, Dictionary<string, List<string>?>? traits = null)
    {
        isUnskippable = false;
        isForcedRun = false;

        if (TestOptimization.Instance.Settings.IntelligentTestRunnerEnabled != true)
        {
            return false;
        }

        var testClassName = runnerInstance.TestClass?.ToString() ?? string.Empty;
        var testMethod = runnerInstance.TestMethod;
        var itrShouldSkip = Common.ShouldSkip(testClassName, testMethod?.Name ?? string.Empty, runnerInstance.TestMethodArguments, testMethod?.GetParameters());
        traits ??= runnerInstance.TestCase.Traits;
        isUnskippable = traits?.TryGetValue(IntelligentTestRunnerTags.UnskippableTraitName, out _) == true;
        isForcedRun = itrShouldSkip && isUnskippable;
        return itrShouldSkip && !isUnskippable;
    }

    internal static void IncrementTotalTestCases()
    {
        Interlocked.Increment(ref _totalTestCases);
    }
}
