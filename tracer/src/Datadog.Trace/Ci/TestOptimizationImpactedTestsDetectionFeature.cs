// <copyright file="TestOptimizationImpactedTestsDetectionFeature.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#nullable enable
using System.Threading.Tasks;
using Datadog.Trace.Ci.CiEnvironment;
using Datadog.Trace.Ci.Configuration;
using Datadog.Trace.Ci.Net;
using Datadog.Trace.Logging;

namespace Datadog.Trace.Ci;

internal class TestOptimizationImpactedTestsDetectionFeature : ITestOptimizationImpactedTestsDetectionFeature
{
    private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(TestOptimizationImpactedTestsDetectionFeature));
    private readonly Task<TestOptimizationClient.ImpactedTestsDetectionResponse> _impactedTestsDetectionFilesTask;
    private readonly CIEnvironmentValues _environmentValues;
    private ImpactedTestsModule? _impactedTestsModule;

    private TestOptimizationImpactedTestsDetectionFeature(TestOptimizationSettings settings, TestOptimizationClient.SettingsResponse clientSettingsResponse, ITestOptimizationClient testOptimizationClient, CIEnvironmentValues environmentValues)
    {
        _environmentValues = environmentValues;
        if (settings.ImpactedTestsDetectionEnabled == null && clientSettingsResponse.ImpactedTestsEnabled.HasValue)
        {
            Log.Information("TestOptimizationImpactedTestsDetectionFeature: Impacted tests detection has been changed to {Value} by the settings api.", clientSettingsResponse.ImpactedTestsEnabled.Value);
            settings.SetImpactedTestsEnabled(clientSettingsResponse.ImpactedTestsEnabled.Value);
        }

        if (settings.ImpactedTestsDetectionEnabled == true)
        {
            Log.Information("TestOptimizationImpactedTestsDetectionFeature: Impacted tests detection is enabled.");
            _impactedTestsDetectionFilesTask = Task.Run(() => InternalGetImpactedTestsDetectionFilesAsync(testOptimizationClient));
            Enabled = true;
        }
        else
        {
            Log.Information("TestOptimizationImpactedTestsDetectionFeature: Impacted tests detection is disabled.");
            _impactedTestsDetectionFilesTask = Task.FromResult(default(TestOptimizationClient.ImpactedTestsDetectionResponse));
            _impactedTestsModule = ImpactedTestsModule.CreateNoOp();
            Enabled = false;
        }

        return;

        static async Task<TestOptimizationClient.ImpactedTestsDetectionResponse> InternalGetImpactedTestsDetectionFilesAsync(ITestOptimizationClient testOptimizationClient)
        {
            Log.Debug("TestOptimizationImpactedTestsDetectionFeature: Getting impacted tests detection modified files...");
            var response = await testOptimizationClient.GetImpactedTestsDetectionFilesAsync().ConfigureAwait(false);
            Log.Debug("TestOptimizationImpactedTestsDetectionFeature: Impacted tests detection modified files received.");
            return response;
        }
    }

    public bool Enabled { get; }

    public TestOptimizationClient.ImpactedTestsDetectionResponse ImpactedTestsDetectionResponse
        => _impactedTestsDetectionFilesTask.SafeGetResult();

    public ImpactedTestsModule ImpactedTestsAnalyzer
    {
        get
        {
            if (_impactedTestsModule is not null)
            {
                return _impactedTestsModule;
            }

            var response = _impactedTestsDetectionFilesTask.SafeGetResult();
            return _impactedTestsModule = ImpactedTestsModule.CreateInstance(response, _environmentValues);
        }
    }

    public static ITestOptimizationImpactedTestsDetectionFeature Create(TestOptimizationSettings settings, TestOptimizationClient.SettingsResponse clientSettingsResponse, ITestOptimizationClient testOptimizationClient, CIEnvironmentValues environmentValues)
        => new TestOptimizationImpactedTestsDetectionFeature(settings, clientSettingsResponse, testOptimizationClient, environmentValues);
}
