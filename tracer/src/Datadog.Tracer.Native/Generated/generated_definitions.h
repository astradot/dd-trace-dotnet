// <copyright company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

#pragma once
#include "../../../../shared/src/native-src/pal.h"
#include "../cor_profiler.h"
#include <map>

namespace trace
{

class GeneratedDefinitions
{
public:
    static int InitCallSites(UINT32 enabledCategories, UINT32 platform);
    static int InitCallTargets(UINT32 enabledCategories, UINT32 platform);
};
} // namespace trace