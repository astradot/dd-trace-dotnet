﻿// <copyright company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
// <auto-generated/>

#nullable enable

using System.Threading;

namespace Datadog.Trace.Telemetry;
internal partial class CiVisibilityMetricsTelemetryCollector
{

    public void RecordCountLogCreated(Datadog.Trace.Telemetry.Metrics.MetricTags.LogLevel tag, int increment = 1)
    {
    }

    public void RecordCountSpanCreated(Datadog.Trace.Telemetry.Metrics.MetricTags.IntegrationName tag, int increment = 1)
    {
    }

    public void RecordCountSpanFinished(int increment = 1)
    {
    }

    public void RecordCountSpanEnqueuedForSerialization(Datadog.Trace.Telemetry.Metrics.MetricTags.SpanEnqueueReason tag, int increment = 1)
    {
    }

    public void RecordCountSpanDropped(Datadog.Trace.Telemetry.Metrics.MetricTags.DropReason tag, int increment = 1)
    {
    }

    public void RecordCountTraceSegmentCreated(Datadog.Trace.Telemetry.Metrics.MetricTags.TraceContinuation tag, int increment = 1)
    {
    }

    public void RecordCountTraceChunkEnqueued(Datadog.Trace.Telemetry.Metrics.MetricTags.TraceChunkEnqueueReason tag, int increment = 1)
    {
    }

    public void RecordCountTraceChunkDropped(Datadog.Trace.Telemetry.Metrics.MetricTags.DropReason tag, int increment = 1)
    {
    }

    public void RecordCountTraceChunkSent(int increment = 1)
    {
    }

    public void RecordCountTraceSegmentsClosed(int increment = 1)
    {
    }

    public void RecordCountTraceApiRequests(int increment = 1)
    {
    }

    public void RecordCountTraceApiResponses(Datadog.Trace.Telemetry.Metrics.MetricTags.StatusCode tag, int increment = 1)
    {
    }

    public void RecordCountTraceApiErrors(Datadog.Trace.Telemetry.Metrics.MetricTags.ApiError tag, int increment = 1)
    {
    }

    public void RecordCountTracePartialFlush(Datadog.Trace.Telemetry.Metrics.MetricTags.PartialFlushReason tag, int increment = 1)
    {
    }

    public void RecordCountContextHeaderStyleInjected(Datadog.Trace.Telemetry.Metrics.MetricTags.ContextHeaderStyle tag, int increment = 1)
    {
    }

    public void RecordCountContextHeaderStyleExtracted(Datadog.Trace.Telemetry.Metrics.MetricTags.ContextHeaderStyle tag, int increment = 1)
    {
    }

    public void RecordCountContextHeaderTruncated(Datadog.Trace.Telemetry.Metrics.MetricTags.ContextHeaderTruncationReason tag, int increment = 1)
    {
    }

    public void RecordCountContextHeaderMalformed(Datadog.Trace.Telemetry.Metrics.MetricTags.ContextHeaderMalformed tag, int increment = 1)
    {
    }

    public void RecordCountStatsApiRequests(int increment = 1)
    {
    }

    public void RecordCountStatsApiResponses(Datadog.Trace.Telemetry.Metrics.MetricTags.StatusCode tag, int increment = 1)
    {
    }

    public void RecordCountStatsApiErrors(Datadog.Trace.Telemetry.Metrics.MetricTags.ApiError tag, int increment = 1)
    {
    }

    public void RecordCountOpenTelemetryConfigHiddenByDatadogConfig(Datadog.Trace.Telemetry.Metrics.MetricTags.DatadogConfiguration tag1, Datadog.Trace.Telemetry.Metrics.MetricTags.OpenTelemetryConfiguration tag2, int increment = 1)
    {
    }

    public void RecordCountOpenTelemetryConfigInvalid(Datadog.Trace.Telemetry.Metrics.MetricTags.DatadogConfiguration tag1, Datadog.Trace.Telemetry.Metrics.MetricTags.OpenTelemetryConfiguration tag2, int increment = 1)
    {
    }

    public void RecordCountTelemetryApiRequests(Datadog.Trace.Telemetry.Metrics.MetricTags.TelemetryEndpoint tag, int increment = 1)
    {
    }

    public void RecordCountTelemetryApiResponses(Datadog.Trace.Telemetry.Metrics.MetricTags.TelemetryEndpoint tag1, Datadog.Trace.Telemetry.Metrics.MetricTags.StatusCode tag2, int increment = 1)
    {
    }

    public void RecordCountTelemetryApiErrors(Datadog.Trace.Telemetry.Metrics.MetricTags.TelemetryEndpoint tag1, Datadog.Trace.Telemetry.Metrics.MetricTags.ApiError tag2, int increment = 1)
    {
    }

    public void RecordCountVersionConflictTracerCreated(int increment = 1)
    {
    }

    public void RecordCountUnsupportedCustomInstrumentationServices(int increment = 1)
    {
    }

    public void RecordCountDirectLogLogs(Datadog.Trace.Telemetry.Metrics.MetricTags.IntegrationName tag, int increment = 1)
    {
    }

    public void RecordCountDirectLogApiRequests(int increment = 1)
    {
    }

    public void RecordCountDirectLogApiResponses(Datadog.Trace.Telemetry.Metrics.MetricTags.StatusCode tag, int increment = 1)
    {
    }

    public void RecordCountDirectLogApiErrors(Datadog.Trace.Telemetry.Metrics.MetricTags.ApiError tag, int increment = 1)
    {
    }

    public void RecordCountWafInit(Datadog.Trace.Telemetry.Metrics.MetricTags.WafStatus tag, int increment = 1)
    {
    }

    public void RecordCountWafUpdates(Datadog.Trace.Telemetry.Metrics.MetricTags.WafStatus tag, int increment = 1)
    {
    }

    public void RecordCountWafRequests(Datadog.Trace.Telemetry.Metrics.MetricTags.WafAnalysis tag, int increment = 1)
    {
    }

    public void RecordCountInputTruncated(Datadog.Trace.Telemetry.Metrics.MetricTags.TruncationReason tag, int increment = 1)
    {
    }

    public void RecordCountRaspRuleEval(Datadog.Trace.Telemetry.Metrics.MetricTags.RaspRuleType tag, int increment = 1)
    {
    }

    public void RecordCountRaspRuleMatch(Datadog.Trace.Telemetry.Metrics.MetricTags.RaspRuleTypeMatch tag, int increment = 1)
    {
    }

    public void RecordCountRaspTimeout(Datadog.Trace.Telemetry.Metrics.MetricTags.RaspRuleType tag, int increment = 1)
    {
    }

    public void RecordCountMissingUserId(Datadog.Trace.Telemetry.Metrics.MetricTags.AuthenticationFrameworkWithEventType tag, int increment = 1)
    {
    }

    public void RecordCountMissingUserLogin(Datadog.Trace.Telemetry.Metrics.MetricTags.AuthenticationFrameworkWithEventType tag, int increment = 1)
    {
    }

    public void RecordCountUserEventSdk(Datadog.Trace.Telemetry.Metrics.MetricTags.UserEventSdk tag, int increment = 1)
    {
    }

    public void RecordCountIastExecutedSources(Datadog.Trace.Telemetry.Metrics.MetricTags.IastSourceType tag, int increment = 1)
    {
    }

    public void RecordCountIastExecutedPropagations(int increment = 1)
    {
    }

    public void RecordCountIastExecutedSinks(Datadog.Trace.Telemetry.Metrics.MetricTags.IastVulnerabilityType tag, int increment = 1)
    {
    }

    public void RecordCountIastRequestTainted(int increment = 1)
    {
    }

    public void RecordCountIastSuppressedVulnerabilities(Datadog.Trace.Telemetry.Metrics.MetricTags.IastVulnerabilityType tag, int increment = 1)
    {
    }
}