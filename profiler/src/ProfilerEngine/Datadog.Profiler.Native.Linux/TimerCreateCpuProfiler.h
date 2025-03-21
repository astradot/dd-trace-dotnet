// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#pragma once

#include "CallstackProvider.h"
#include "CounterMetric.h"
#include "ManagedThreadInfo.h"
#include "MetricsRegistry.h"
#include "ServiceBase.h"

#include <signal.h>

#include <atomic>
#include <chrono>
#include <memory>
#include <shared_mutex>
#include <unordered_set>

class DiscardMetrics;
class IConfiguration;
class IThreadInfo;
class IManagedThreadList;
class ProfilerSignalManager;
class CpuTimeProvider;
class CallstackProvider;

class TimerCreateCpuProfiler : public ServiceBase
{
public:
    TimerCreateCpuProfiler(
        IConfiguration* pConfiguration,
        ProfilerSignalManager* pSignalManager,
        IManagedThreadList* pManagedThreadsList,
        CpuTimeProvider* pProvider,
        CallstackProvider calstackProvider,
        MetricsRegistry& metricsRegistry) noexcept;

    ~TimerCreateCpuProfiler();

    void RegisterThread(std::shared_ptr<ManagedThreadInfo> threadInfo);
    void UnregisterThread(std::shared_ptr<ManagedThreadInfo> threadInfo);

    const char* GetName() override;

private:
    static bool CollectStackSampleSignalHandler(int sig, siginfo_t* info, void* ucontext);
    static TimerCreateCpuProfiler* Instance;

    bool CanCollect(void* context);
    bool Collect(void* ucontext);
    void RegisterThreadImpl(ManagedThreadInfo* thread);
    void UnregisterThreadImpl(ManagedThreadInfo* threadInfo);

    bool StartImpl() override;
    bool StopImpl() override;

    ProfilerSignalManager* _pSignalManager;
    IManagedThreadList* _pManagedThreadsList;
    CpuTimeProvider* _pProvider;
    CallstackProvider _callstackProvider;
    std::chrono::milliseconds _samplingInterval;
    std::shared_mutex _registerLock;
    std::shared_ptr<CounterMetric> _totalSampling;
    std::shared_ptr<DiscardMetrics> _discardMetrics;
};