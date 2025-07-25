﻿#pragma once
#include "../../../shared/src/native-src/com_ptr.h"
#include <string>
#include "cor_profiler.h"

namespace datadog::shared::nativeloader
{
class SingleStepGuardRails
{
private:
    bool m_isRunningInSingleStep;
    bool m_isForcedExecution = false;
    std::string m_injectResult;
    std::string m_injectResultReason;
    std::string m_injectResultClass;

    bool ShouldForceInstrumentationOverride(const std::string& eolDescription, bool isEol);
    HRESULT HandleUnsupportedNetCoreVersion(const std::string& unsupportedDescription, const std::string& runtimeVersion, const bool isEol);
    HRESULT HandleUnsupportedNetFrameworkVersion(const std::string& unsupportedDescription, const std::string& runtimeVersion, const bool isEol);

    void SendAbortTelemetry(const std::string& runtimeName, const std::string& runtimeVersion, const bool isEol) const;
    void SendTelemetry(const std::string& runtimeName, const std::string& runtimeVersion, const std::string& telemetryPoints) const;
public:
    inline static const std::string NetFrameworkRuntime = ".NET Framework";
    inline static const std::string NetCoreRuntime = ".NET Core";

    SingleStepGuardRails();
    ~SingleStepGuardRails();
    HRESULT CheckRuntime(const RuntimeInformation& runtimeInformation, IUnknown* pICorProfilerInfoUnk);
    void RecordBootstrapError(const std::string& runtimeName, const std::string& runtimeVersion, const std::string& errorType) const;
    void RecordBootstrapError(const RuntimeInformation& runtimeInformation, const std::string& errorType);
    void RecordBootstrapSuccess(const RuntimeInformation& runtimeInformation) const;
    void SetInjectResult(const std::string& result, const std::string& resultReason, const std::string& resultClass);

};
} // namespace datadog::shared::nativeloader