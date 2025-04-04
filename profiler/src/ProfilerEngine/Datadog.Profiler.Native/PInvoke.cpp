// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#include "PInvoke.h"
#include "CorProfilerCallback.h"
#include "IClrLifetime.h"
#include "Log.h"
#include "ManagedThreadList.h"
#include "ProfilerEngineStatus.h"
#include "ThreadsCpuManager.h"

#include "shared/src/native-src/loader.h"

extern "C" void __stdcall ThreadsCpuManager_Map(std::uint32_t threadId, const WCHAR* pName)
{
    const auto profiler = CorProfilerCallback::GetInstance();
    if (profiler == nullptr)
    {
        Log::Error("ThreadsCpuManager_Map is called BEFORE CLR initialize");
        return;
    }

    profiler->GetThreadsCpuManager()->Map(threadId, pName);
}

extern "C" void* __stdcall GetNativeProfilerIsReadyPtr()
{
    const auto profiler = CorProfilerCallback::GetInstance();

    if (profiler == nullptr)
    {
        Log::Error("GetNativeProfilerIsReadyPtr is called BEFORE CLR initialize");
        return nullptr;
    }

    if (!profiler->GetClrLifetime()->IsRunning())
    {
        return nullptr;
    }

    return (void*)ProfilerEngineStatus::GetReadPtrIsProfilerEngineActive();
}

extern "C" void* __stdcall GetPointerToNativeTraceContext()
{
    const auto profiler = CorProfilerCallback::GetInstance();

    if (profiler == nullptr)
    {
        Log::Error("GetPointerToNativeTraceContext is called BEFORE CLR initialize");
        return nullptr;
    }

    if (!profiler->GetClrLifetime()->IsRunning())
    {
        return nullptr;
    }

    profiler->TraceContextHasBeenSet();

    // Engine is active. Get info for current thread.
    auto pCurrentThreadInfo = ManagedThreadInfo::CurrentThreadInfo;
    if (pCurrentThreadInfo == nullptr)
    {
        // There was an error looking up the current thread info:
        return nullptr;
    }

    profiler->GetCodeHotspotThreadList()->RegisterThread(pCurrentThreadInfo);

    // Get pointers to the relevant fields within the thread info data structure.
    return pCurrentThreadInfo->GetTraceContextPointer();
}

extern "C" void __stdcall SetApplicationInfoForAppDomain(const char* runtimeId, const char* serviceName, const char* environment, const char* version)
{
    const auto profiler = CorProfilerCallback::GetInstance();

    if (profiler == nullptr)
    {
        Log::Error("SetApplicationInfo is called BEFORE CLR initialize");
        return;
    }

    if (!profiler->GetClrLifetime()->IsRunning())
    {
        return;
    }

    profiler->GetApplicationStore()->SetApplicationInfo(
        runtimeId ? runtimeId : std::string(),
        serviceName ? serviceName : std::string(),
        environment ? environment : std::string(),
        version ? version : std::string());
}

extern "C" void __stdcall SetEndpointForTrace(const char* runtimeId, uint64_t traceId, const char* endpoint)
{
    const auto profiler = CorProfilerCallback::GetInstance();

    if (profiler == nullptr)
    {
        Log::Error("SetEndpointForTrace is called BEFORE CLR initialize");
        return;
    }

    if (!profiler->GetClrLifetime()->IsRunning())
    {
        return;
    }

    static bool firstEmptyRuntimeId = true;
    if (runtimeId == nullptr)
    {
        if (firstEmptyRuntimeId)
        {
            Log::Error("SetEndpointForTrace was called with an empty runtime id");
            firstEmptyRuntimeId = false;
        }
        return;
    }

    static bool firstEmptyEndpoint = true;
    if (endpoint == nullptr)
    {
        if (firstEmptyEndpoint)
        {
            // It could happen that the endpoint is empty, but the tracer should check before making the call,
            // to avoid the cost of the p/invoke
            Log::Warn("SetEndpointForTrace was called with an empty endpoint");
            firstEmptyEndpoint = false;
        }
        return;
    }

    profiler->GetExporter()->SetEndpoint(runtimeId, traceId, endpoint);
}

extern "C" void __stdcall SetGitMetadataForApplication(const char* runtimeId, const char* repositoryUrl, const char* commitSha)
{
    const auto profiler = CorProfilerCallback::GetInstance();

    if (profiler == nullptr)
    {
        Log::Error("SetGitMetadataForApplication is called BEFORE CLR initialize");
        return;
    }

    if (!profiler->GetClrLifetime()->IsRunning())
    {
        return;
    }

    static bool firstEmptyRuntimeId = true;
    if (runtimeId == nullptr)
    {
        if (firstEmptyRuntimeId)
        {
            Log::Error("SetGitMetadataForApplication was called with an empty runtime id");
            firstEmptyRuntimeId = false;
        }
        return;
    }

    profiler->GetApplicationStore()->SetGitMetadata(
        runtimeId,
        repositoryUrl != nullptr ? repositoryUrl : std::string(),
        commitSha != nullptr ? commitSha : std::string()
    );
}


extern "C" void __stdcall FlushProfile()
{
    const auto profiler = CorProfilerCallback::GetInstance();

    if (profiler == nullptr)
    {
        Log::Error("FlushProfile is called BEFORE CLR initialize");
        return;
    }

    if (!profiler->GetClrLifetime()->IsRunning())
    {
        return;
    }

    Log::Debug("FlushProfile called by Managed code");
    profiler->GetSamplesCollector()->Export();
}