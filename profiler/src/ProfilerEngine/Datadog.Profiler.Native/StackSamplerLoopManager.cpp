// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#include "StackSamplerLoopManager.h"

#include "IClrLifetime.h"
#include "OpSysTools.h"
#include "OsSpecificApi.h"
#include "ThreadsCpuManager.h"

using namespace std::chrono_literals;

constexpr std::chrono::milliseconds DeadlockDetectionInterval = 1s;
constexpr std::chrono::milliseconds MaxExpectedStackSampleCollectionDurationMs = 500ms;
constexpr std::chrono::nanoseconds CollectionDurationThresholdNs = std::chrono::nanoseconds(MaxExpectedStackSampleCollectionDurationMs);

#ifdef NDEBUG
constexpr std::chrono::milliseconds StatsAggregationPeriodMs = 30000ms;
#else
constexpr std::chrono::milliseconds StatsAggregationPeriodMs = 10000ms;
#endif
constexpr std::chrono::nanoseconds StatsAggregationPeriodNs = StatsAggregationPeriodMs;

const WCHAR* WatcherThreadName = WStr("DD_Watcher");
const std::chrono::nanoseconds StackSamplerLoopManager::StatisticAggregationPeriodNs = 10s;

StackSamplerLoopManager::StackSamplerLoopManager(
    ICorProfilerInfo4* pCorProfilerInfo,
    IConfiguration* pConfiguration,
    std::shared_ptr<IMetricsSender> metricsSender,
    IClrLifetime const* clrLifetime,
    IThreadsCpuManager* pThreadsCpuManager,
    IManagedThreadList* pManagedThreadList,
    IManagedThreadList* pCodeHotspotThreadList,
    ICollector<RawWallTimeSample>* pWallTimeCollector,
    ICollector<RawCpuSample>* pCpuTimeCollector,
    MetricsRegistry& metricsRegistry,
    CallstackProvider callstackProvider
    ) :
    _pCorProfilerInfo{pCorProfilerInfo},
    _pConfiguration{pConfiguration},
    _pThreadsCpuManager{pThreadsCpuManager},
    _pManagedThreadList{pManagedThreadList},
    _pCodeHotspotsThreadList{pCodeHotspotThreadList},
    _pWallTimeCollector{pWallTimeCollector},
    _pCpuTimeCollector{pCpuTimeCollector},
    _pStackFramesCollector{nullptr},
    _pStackSamplerLoop{nullptr},
    _deadlockInterventionInProgress{0},
    _pWatcherThread{nullptr},
    _isWatcherShutdownRequested{false},
    _pTargetThread{nullptr},
    _collectionStartNs{0},
    _isTargetThreadSuspended{false},
    _isForceTerminated{false},
    _currentPeriod{0},
    _currentPeriodStartNs{0},
    _deadlocksInPeriod{0},
    _totalDeadlockDetectionsCount{0},
    _metricsSender{metricsSender},
    _statisticsReadyToSend{nullptr},
    _metricsRegistry{metricsRegistry},
    _callstackProvider{std::move(callstackProvider)}
{
    _pCorProfilerInfo->AddRef();
    _pStackFramesCollector = OsSpecificApi::CreateNewStackFramesCollectorInstance(
        _pCorProfilerInfo, pConfiguration, &_callstackProvider, _metricsRegistry);

    _currentStatistics = std::make_unique<Statistics>();
    _statisticCollectionStartNs = OpSysTools::GetHighPrecisionNanoseconds();

    _deadlockCountMetric = metricsRegistry.GetOrRegister<CounterMetric>("dotnet_internal_deadlocks");
}

StackSamplerLoopManager::~StackSamplerLoopManager()
{
    // Just in case it was not called explicitely
    Stop();

    ICorProfilerInfo4* pCorProfilerInfo = _pCorProfilerInfo;
    if (pCorProfilerInfo != nullptr)
    {
        pCorProfilerInfo->Release();
        _pCorProfilerInfo = nullptr;
    }
}

const char* StackSamplerLoopManager::GetName()
{
    return _serviceName;
}

bool StackSamplerLoopManager::StartImpl()
{
    InitializeSampler();
    RunWatcherAndSampler();

    return true;
}

bool StackSamplerLoopManager::StopImpl()
{
    _pStackSamplerLoop->Stop();

    ShutdownWatcher();

    return true;
}

void StackSamplerLoopManager::InitializeSampler()
{
    _pStackSamplerLoop = std::make_unique<StackSamplerLoop>(
        _pCorProfilerInfo,
        _pConfiguration,
        _pStackFramesCollector.get(),
        this,
        _pThreadsCpuManager,
        _pManagedThreadList,
        _pCodeHotspotsThreadList,
        _pWallTimeCollector,
        _pCpuTimeCollector,
        _metricsRegistry);
}

void StackSamplerLoopManager::RunWatcherAndSampler()
{
    _pWatcherThread = std::make_unique<std::thread>([this]
        {
            OpSysTools::SetNativeThreadName(WatcherThreadName);
            WatcherLoop();
        });
}

void StackSamplerLoopManager::ShutdownWatcher()
{
    if (_pWatcherThread != nullptr)
    {
        _isWatcherShutdownRequested = true;

        _pWatcherThread->join();
    }
}

void StackSamplerLoopManager::WatcherLoop()
{
    Log::Info("StackSamplerLoopManager::WatcherLoop started.");
    _pThreadsCpuManager->Map(OpSysTools::GetThreadId(), WatcherThreadName);

    // Start the sampler loop only when the watcher is ready
    _pStackSamplerLoop->Start();

    while (false == _isWatcherShutdownRequested)
    {
        try
        {
            std::this_thread::sleep_for(DeadlockDetectionInterval);

            WatcherLoopIteration();
            SendStatistics();
        }
        catch (const std::runtime_error& re)
        {
            Log::Error("Runtime error in StackSamplerLoopManager::WatcherLoop: ", re.what());
        }
        catch (const std::exception& ex)
        {
            Log::Error("Typed Exception in StackSamplerLoopManager::WatcherLoop: ", ex.what());
        }
        catch (...)
        {
            Log::Error("Unknown Exception in StackSamplerLoopManager::WatcherLoop.");
        }
    }

    Log::Info("StackSamplerLoopManager::WatcherLoop finished.");
}

void StackSamplerLoopManager::SendStatistics()
{
    // could be null on customer site (metrics are only sent in Reliability Environment)
    if (_metricsSender == nullptr || _statisticsReadyToSend == nullptr)
        return;

    _metricsSender->Gauge(Statistics::MeanSuspensionTimeMetricName, _statisticsReadyToSend->GetMeanSuspensionTime());
    _metricsSender->Gauge(Statistics::MaxSuspensionTimeMetricName, _statisticsReadyToSend->GetMaxSuspensionTime());
    _metricsSender->Gauge(Statistics::MeanCollectionTimeMetricName, _statisticsReadyToSend->GetMeanCollectionTime());
    _metricsSender->Gauge(Statistics::MaxCollectionTimeMetricName, _statisticsReadyToSend->GetMaxCollectionTime());
    _metricsSender->Counter(Statistics::TotalDeadlocksMetricName, _statisticsReadyToSend->GetTotalDeadlocks());

    Log::Debug("Sampling metrics have been sent. ");

    _statisticsReadyToSend.reset();
}

void StackSamplerLoopManager::WatcherLoopIteration()
{
    std::lock_guard<std::mutex> guardedLock(_watcherActivityLock);

    // Check whether the statistics aggregation period has completed.
    // If yes, reset the counters and log the stats, if the correspionding conditions are met.
    std::int64_t currentNanosecs = OpSysTools::GetHighPrecisionNanoseconds();
    std::chrono::nanoseconds periodDurationNs =
        (_currentPeriodStartNs == 0)
            ? 0ns
            : std::chrono::nanoseconds(currentNanosecs - _currentPeriodStartNs);

    if (_currentPeriodStartNs == 0 || periodDurationNs > StatsAggregationPeriodNs)
    {
        StartNewStatsAggregationPeriod(currentNanosecs, periodDurationNs);
    }

    // Check whether a stack sample collection is ongoing
    if (_collectionStartNs == 0)
    {
        return;
    }

    if (_deadlockInterventionInProgress >= 1)
    {
        // TODO: Validate that calling resuming again (and again) could unlock the situation.
        // The previous call to ResumeThread failed.
        if (_deadlockInterventionInProgress == 1)
        {
            _deadlockInterventionInProgress++;
            Log::Info("StackSamplerLoopManager::WatcherLoopIteration - Deadlock intervention still in progress for thread ", _pTargetThread->GetOsThreadId(),
                       std::hex, " (= 0x", _pTargetThread->GetOsThreadId(), ")");
        }
        return;
    }

    // ! ! ! ! ! ! ! ! ! ! !
    // At this point we know that a collection is ongoing.
    // This means that the target thread MAY be either already suspended or may be suspended any time:
    //
    // The StackSamplerLoop has already called AllowStackWalk(..)
    // (because pCurrentStackSampleCollectionThreadInfo is not nullptr).
    // But at this point we have not yet checked if (false == _isTargetThreadSuspended),
    // so we do not know whether StackSamplerLoop is about to suspend the target thread, has already suspended it,
    // or already called SuspendTargetThreadIfRequired(..) and decided not to suspend it. Whatever the case, it may
    // happen concurrently because any of those actions does not require to be done under the _watcherActivityLock.
    //
    // So we must behave according to the thread-suspended mode:
    //    No logging, no allocations, no APIs that might allocate, log or take any shared or global locks.
    //
    // The LogDuringStackSampling_Unsafe switch allows to enable some UNSAFE logging.
    // ONLY USE IT TO DEBUG ISSUES WITH POSSIBLE DEADLOCK.
    // ! ! ! ! ! ! ! ! ! ! !

    std::chrono::nanoseconds collectionDurationNs = std::chrono::nanoseconds(currentNanosecs - _collectionStartNs);
    if (collectionDurationNs <= CollectionDurationThresholdNs)
    {
        // under the termination threshold
        return;
    }

#ifdef _WINDOWS
    auto samplerThreadhandle = static_cast<HANDLE>(_pStackSamplerLoop->_pLoopThread->native_handle());

    FILETIME creationTime, exitTime, kernelTime, userTime;
    GetThreadTimes(samplerThreadhandle, &creationTime, &exitTime, &kernelTime, &userTime);

    if (HasMadeProgress(userTime, kernelTime))
    {
        _userTime = userTime;
        _kernelTime = kernelTime;
        return;
    }
#endif

    _deadlockCountMetric->Incr();
    _currentStatistics->IncrDeadlockCount();

    PerformDeadlockIntervention(collectionDurationNs);
}

bool StackSamplerLoopManager::HasMadeProgress(FILETIME userTime, FILETIME kernelTime)
{
    return userTime.dwLowDateTime != _userTime.dwLowDateTime ||
           userTime.dwHighDateTime != _userTime.dwHighDateTime ||
           kernelTime.dwLowDateTime != _kernelTime.dwLowDateTime ||
           kernelTime.dwHighDateTime != _kernelTime.dwHighDateTime;
}

void StackSamplerLoopManager::PerformDeadlockIntervention(const std::chrono::nanoseconds& ongoingStackSampleCollectionDurationNs)
{
    _deadlockInterventionInProgress = 1;

    // ! ! ! ! ! ! ! ! ! ! !
    // This private method is invoked by WatcherLoopIteration().
    // It MUST be invoked ONLY while holding the _watcherActivityLock.
    //
    // When this method is invoked, we know that the profiling target thread is suspended.
    // Therefore, all restrictions applicable to the thread-suspended mode apply:
    // No logging, no allocations, no APIs that might allocate, log or take any shared or global locks.
    // ! ! ! ! ! ! ! ! ! ! !

    // ** Collect statistics about this deadlock:

    // Determine if the thread was fit for collection previously
    // (this will also reset it's internal state for the current period if applicable)
    bool wasThreadSafeForStackSampleCollection = GetUpdateIsThreadSafeForStackSampleCollection(_pTargetThread.get(), nullptr);

    _pTargetThread->IncDeadlocksCount();
    _deadlocksInPeriod++;
    _totalDeadlockDetectionsCount++;

    // Determine if the thread is fit for future collections. If this status changed, we will log it when safe.
    bool isThreadSafeForStackSampleCollection = GetUpdateIsThreadSafeForStackSampleCollection(_pTargetThread.get(), nullptr);

    _pStackFramesCollector->RequestAbortCurrentCollection();

    // resume target thread
    uint32_t hr;
    _pStackFramesCollector->ResumeTargetThreadIfRequired(_pTargetThread.get(),
                                                         _isTargetThreadSuspended,
                                                         &hr);

    // don't forget to resume the target thread if needed (required in 32 bit)
    _pStackFramesCollector->OnDeadlock();

    LogDeadlockIntervention(ongoingStackSampleCollectionDurationNs,
                            wasThreadSafeForStackSampleCollection,
                            isThreadSafeForStackSampleCollection,
                            SUCCEEDED(hr));

    // The sampled thread has been resumed. The lock blocking the stack sampler loop should be released anytime soon.
}

void StackSamplerLoopManager::LogDeadlockIntervention(const std::chrono::nanoseconds& ongoingStackSampleCollectionDurationNs,
                                                      bool wasThreadSafeForStackSampleCollection,
                                                      bool isThreadSafeForStackSampleCollection,
                                                      bool isThreadResumed)
{
    // ** Log a notice of this intervention:
    // (only if we successfully resumed the target thread, as it is not safe otherwise)

    std::uint64_t threadDeadlockTotalCount;
    std::uint64_t threadDeadlockInAggPeriodCount;
    std::uint64_t threadUsedDeadlocksAggPeriodIndex;
    _pTargetThread->GetDeadlocksCount(&threadDeadlockTotalCount,
                                      &threadDeadlockInAggPeriodCount,
                                      &threadUsedDeadlocksAggPeriodIndex);

    Log::Info("StackSamplerLoopManager::PerformDeadlockIntervention(): The ongoing StackSampleCollection duration crossed the threshold."
              " A deadlock intervention was performed."
              " Deadlocked target thread=(OsThreadId=", std::dec, _pTargetThread->GetOsThreadId(), ", ",
              " ClrThreadId=0x", std::hex, _pTargetThread->GetClrThreadId(), ");", std::dec,
              " ongoingStackSampleCollectionDurationNs=", ToMillis(ongoingStackSampleCollectionDurationNs), " millisecs;",
              " _isTargetThreadResumed=", std::boolalpha, isThreadResumed, ";",
              " _currentPeriod=", _currentPeriod, ";",
              " _deadlocksInPeriod=", _deadlocksInPeriod, ";",
              " _totalDeadlockDetectionsCount=", _totalDeadlockDetectionsCount, ";",
              " wasThreadSafeForStackSampleCollection=", std::boolalpha, wasThreadSafeForStackSampleCollection, ";",
              " isThreadSafeForStackSampleCollection=", isThreadSafeForStackSampleCollection, ";", std::noboolalpha,
              " threadDeadlockTotalCount=", threadDeadlockTotalCount, ";",
              " threadDeadlockInAggPeriodCount=", threadDeadlockInAggPeriodCount, ";",
              " threadUsedDeadlocksAggPeriodIndex=", threadUsedDeadlocksAggPeriodIndex, ".");

    if (wasThreadSafeForStackSampleCollection != isThreadSafeForStackSampleCollection)
    {
        Log::Info("ShouldCollectThread status changed in PerformDeadlockIntervention"
                  " for thread (OsThreadId=", _pTargetThread->GetOsThreadId(),
                  ", ClrThreadId=0x", std::hex, _pTargetThread->GetClrThreadId(), std::dec,
                  ", ThreadName=\"", _pTargetThread->GetThreadName(),
                  " wasThreadSafeForStackSampleCollection=", std::boolalpha, wasThreadSafeForStackSampleCollection, ";",
                  " isThreadSafeForStackSampleCollection=", isThreadSafeForStackSampleCollection, ";", std::noboolalpha,
                  " threadDeadlockTotalCount=", threadDeadlockTotalCount, ";",
                  " threadDeadlockInAggPeriodCount=", threadDeadlockInAggPeriodCount, ";",
                  " threadUsedDeadlocksAggPeriodIndex=", threadUsedDeadlocksAggPeriodIndex, ";",
                  " _deadlocksInPeriod=", _deadlocksInPeriod, ".");
    }
}

void StackSamplerLoopManager::StartNewStatsAggregationPeriod(std::int64_t currentHighPrecisionNanosecs,
                                                             const std::chrono::nanoseconds& periodDurationNs)
{
    // This method must only be called while _watcherActivityLock is held!

    // We log the stats only if there is no ongoing stack sampling going on
    // (as we are then guaranteed not to get into suspended thread caused deadlock)
    // or if unsafe logging during stack walks was explicitly opted into.
    if (LogDuringStackSampling_Unsafe || _pTargetThread == nullptr)
    {
        // Do not flood logs:
        // If we detected issues in this period, always log an info message,
        if (_deadlocksInPeriod > 0)
        {
            Log::Info("StackSamplerLoopManager: Completing a StatsAggregationPeriod.",
                      " Period-Index=", _currentPeriod, ",",
                      " Targeted-PeriodDuration=", StatsAggregationPeriodMs.count(), " millisec,",
                      " Actual-PeriodDuration=", ToMillis(periodDurationNs), " millisec,",
                      " Period-DeadlockDetectionsCount=", _deadlocksInPeriod, ",",
                      " AppLifetime-DeadlockDetectionsCount=", _totalDeadlockDetectionsCount, ".");
        }
    }

    _currentPeriod++;
    _currentPeriodStartNs = currentHighPrecisionNanosecs;
    _deadlocksInPeriod = 0;
}

double StackSamplerLoopManager::ToMillis(const std::chrono::nanoseconds& nanosecs)
{
    return nanosecs.count() / 1000000.0;
}

bool StackSamplerLoopManager::AllowStackWalk(std::shared_ptr<ManagedThreadInfo> pThreadInfo)
{
    std::lock_guard<std::mutex> guardedLock(_watcherActivityLock);

    bool isThreadSafeStatusChanged;
    bool isThreadSafeForStackSampleCollection = GetUpdateIsThreadSafeForStackSampleCollection(pThreadInfo.get(), &isThreadSafeStatusChanged);

    if (isThreadSafeStatusChanged)
    {
        std::uint64_t threadDeadlockTotalCount;
        std::uint64_t threadDeadlockInAggPeriodCount;
        std::uint64_t threadUsedDeadlocksAggPeriodIndex;
        pThreadInfo->GetDeadlocksCount(&threadDeadlockTotalCount,
                                       &threadDeadlockInAggPeriodCount,
                                       &threadUsedDeadlocksAggPeriodIndex);

        // At that step, the target thread is not suspended so no deadlock risk when logging
        Log::Info("ShouldCollectThread status changed in AllowStackWalk",
                  " for thread (OsThreadId=", pThreadInfo->GetOsThreadId(),
                  ", ClrThreadId=0x", std::hex, pThreadInfo->GetClrThreadId(), std::dec,
                  ", ThreadName=\"", pThreadInfo->GetThreadName(), "\"):",
                  " ShouldCollectThread=", isThreadSafeForStackSampleCollection, ";",
                  " threadDeadlockTotalCount=", threadDeadlockTotalCount, ";",
                  " threadDeadlockInAggPeriodCount=", threadDeadlockInAggPeriodCount, ";",
                  " threadUsedDeadlocksAggPeriodIndex=", threadUsedDeadlocksAggPeriodIndex, ";",
                  " _deadlocksInPeriod=", _deadlocksInPeriod, ".");
    }


    if (!isThreadSafeForStackSampleCollection)
    {
        return false;
    }

    // According to
    // https://sourcegraph.com/github.com/dotnet/runtime/-/blob/src/coreclr/vm/proftoeeinterfaceimpl.cpp?L8479
    // we _must_ block in ICorProfilerCallback::ThreadDestroyed to prevent the thread from being destroyed
    // while walking its callstack.
    pThreadInfo->AcquireLock();

    // We _must_ check if the thread was not destroyed while acquiring the lock
    if (pThreadInfo->IsDestroyed())
    {
        pThreadInfo->ReleaseLock();
        return false;
    }

    _pTargetThread = std::move(pThreadInfo);
    _isTargetThreadSuspended = false;
    _isForceTerminated = false;

    return true;
}

void StackSamplerLoopManager::NotifyThreadState(bool isSuspended)
{
    std::lock_guard<std::mutex> guardedLock(_watcherActivityLock);

    _isTargetThreadSuspended = isSuspended;
    _threadSuspensionStart = OpSysTools::GetHighPrecisionNanoseconds();
}

void StackSamplerLoopManager::NotifyCollectionStart()
{
    std::lock_guard<std::mutex> guardedLock(_watcherActivityLock);

    _collectionStartNs = OpSysTools::GetHighPrecisionNanoseconds();

#ifdef _WINDOWS
    auto samplerThreadhandle = static_cast<HANDLE>(_pStackSamplerLoop->_pLoopThread->native_handle());
    FILETIME creationTime, exitTime;
    GetThreadTimes(samplerThreadhandle, &creationTime, &exitTime, &_kernelTime, &_userTime);
#endif
}

void StackSamplerLoopManager::NotifyCollectionEnd()
{
    std::lock_guard<std::mutex> guardedLock(_watcherActivityLock);

    std::int64_t collectionEndTimeNs = OpSysTools::GetHighPrecisionNanoseconds();
    auto collectionDuration = collectionEndTimeNs - _collectionStartNs;

    // TODO: update colletion time metric when available

    _currentStatistics->AddCollectionTime(collectionDuration);

    _collectionStartNs = 0;
    _kernelTime = {0};
    _userTime = {0};
    _deadlockInterventionInProgress = 0;
}

void StackSamplerLoopManager::NotifyIterationFinished()
{
    std::lock_guard<std::mutex> guardedLock(_watcherActivityLock);

    _pTargetThread->ReleaseLock();
    _pTargetThread.reset();
    _collectionStartNs = 0;
    _isTargetThreadSuspended = false;

    std::int64_t threadCollectionEndTimeNs = OpSysTools::GetHighPrecisionNanoseconds();
    auto suspensionDuration = threadCollectionEndTimeNs - _threadSuspensionStart;

    // TODO: update suspension time metric when available

    _currentStatistics->AddSuspensionTime(suspensionDuration);

    if (_metricsSender != nullptr && threadCollectionEndTimeNs - _statisticCollectionStartNs >= StatisticAggregationPeriodNs.count())
    {
        Log::Debug("Notify-ThreadStackSampleCollection-Finished invoked - Prepare statistics to be sent.");
        _statisticsReadyToSend.reset(_currentStatistics.release());
        _currentStatistics = std::make_unique<Statistics>();
        _statisticCollectionStartNs = threadCollectionEndTimeNs;
    }
}

inline bool StackSamplerLoopManager::GetUpdateIsThreadSafeForStackSampleCollection(
    ManagedThreadInfo* pThreadInfo,
    bool* pIsStatusChanged)
{
    // This method must only be called while _watcherActivityLock is held!

    std::uint64_t prevAggPeriodDeadlockCount;
    std::uint64_t currAggPeriodDeadlockCount;
    pThreadInfo->GetOrResetDeadlocksCount(
        _currentPeriod,
        &prevAggPeriodDeadlockCount,
        &currAggPeriodDeadlockCount);

    bool wasThreadSafeForStackSampleCollection =
        ShouldCollectThread(
            prevAggPeriodDeadlockCount,
            _deadlocksInPeriod);
    bool isThreadSafeForStackSampleCollection =
        ShouldCollectThread(
            currAggPeriodDeadlockCount,
            _deadlocksInPeriod);

    if (pIsStatusChanged != nullptr)
    {
        *pIsStatusChanged = (wasThreadSafeForStackSampleCollection != isThreadSafeForStackSampleCollection);
    }

    return isThreadSafeForStackSampleCollection;
}

inline bool StackSamplerLoopManager::ShouldCollectThread(
    std::uint64_t threadAggPeriodDeadlockCount,
    std::uint64_t globalAggPeriodDeadlockCount)
{
    return (threadAggPeriodDeadlockCount <= DeadlocksPerThreadThreshold) &&
           (globalAggPeriodDeadlockCount <= TotalDeadlocksThreshold);
}
