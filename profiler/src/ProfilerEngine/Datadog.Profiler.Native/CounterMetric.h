// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2022 Datadog, Inc.

#pragma once

#include "MetricBase.h"

#include <atomic>
#include <string>

class CounterMetric : public MetricBase
{
public:
    CounterMetric(std::string name);

    void Incr();

    std::list<Metric> GetMetrics() override;

private:
    std::atomic<uint64_t> _count;
};