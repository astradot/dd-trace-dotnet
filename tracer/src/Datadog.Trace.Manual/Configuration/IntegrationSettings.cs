﻿// <copyright file="IntegrationSettings.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Runtime.CompilerServices;
using Datadog.Trace.ClrProfiler.AutoInstrumentation.ManualInstrumentation;
using Datadog.Trace.SourceGenerators;

namespace Datadog.Trace.Configuration;

/// <summary>
/// Contains integration-specific settings.
/// </summary>
public sealed class IntegrationSettings
{
    private OverrideValue<bool?> _enabled;
    private OverrideValue<bool?> _analyticsEnabled;
    private OverrideValue<double> _analyticsSampleRate;

    internal IntegrationSettings(string integrationName, bool? enabled, bool? analyticsEnabled, double analyticsSampleRate)
    {
        IntegrationName = integrationName;
        _enabled = new(enabled);
        _analyticsEnabled = new(analyticsEnabled);
        _analyticsSampleRate = new(analyticsSampleRate);
    }

    /// <summary>
    /// Gets the name of the integration. Used to retrieve integration-specific settings.
    /// </summary>
    public string IntegrationName
    {
        [Instrumented]
        [MethodImpl(MethodImplOptions.NoInlining)]
        get;
    }

    /// <summary>
    /// Gets or sets a value indicating whether
    /// this integration is enabled.
    /// </summary>
    public bool? Enabled
    {
        [Instrumented]
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => _enabled.Value;
        set => _enabled = _enabled.Override(value);
    }

    /// <summary>
    /// Gets or sets a value indicating whether
    /// Analytics are enabled for this integration.
    /// </summary>
    public bool? AnalyticsEnabled
    {
        [Instrumented]
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => _analyticsEnabled.Value;
        set => _analyticsEnabled = _analyticsEnabled.Override(value);
    }

    /// <summary>
    /// Gets or sets a value between 0 and 1 (inclusive)
    /// that determines the sampling rate for this integration.
    /// </summary>
    public double AnalyticsSampleRate
    {
        [Instrumented]
        [MethodImpl(MethodImplOptions.NoInlining)]
        get => _analyticsSampleRate.Value;
        set => _analyticsSampleRate = _analyticsSampleRate.Override(value);
    }

    /// <summary>
    /// "Serializes" the values, if changed, to an array. If there are no updates, returns null
    /// </summary>
    internal object?[]? GetChangeDetails()
        => IntegrationSettingsSerializationHelper.SerializeFromManual(
            _enabled.IsOverridden,
            _enabled.IsOverridden ? _enabled.Value : null,
            _analyticsEnabled.IsOverridden,
            _analyticsEnabled.IsOverridden ? _analyticsEnabled.Value : null,
            _analyticsSampleRate.IsOverridden,
            _analyticsSampleRate.IsOverridden ? _analyticsSampleRate.Value : null);

    internal void RecordChangedKeys(Dictionary<string, object?> results)
    {
        if (_enabled.IsOverridden)
        {
            results[$"DD_TRACE_{IntegrationName.ToUpperInvariant()}_ENABLED"] = _enabled.Value;
        }

        if (_analyticsEnabled.IsOverridden)
        {
            results[$"DD_TRACE_{IntegrationName.ToUpperInvariant()}_ANALYTICS_ENABLED"] = _analyticsEnabled.Value;
        }

        if (_analyticsSampleRate.IsOverridden)
        {
            results[$"DD_TRACE_{IntegrationName.ToUpperInvariant()}_ANALYTICS_SAMPLE_RATE"] = _analyticsSampleRate.Value;
        }
    }
}
