// <copyright file="TestCategoryAttributeStruct.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#nullable enable

using System.Collections.Generic;
using Datadog.Trace.DuckTyping;

#pragma warning disable CS0649 // Field 'TestCategories' is never assigned to

namespace Datadog.Trace.ClrProfiler.AutoInstrumentation.Testing.MsTestV2
{
    /// <summary>
    /// TestCategoryAttribute ducktype struct
    /// </summary>
    [DuckCopy]
    internal struct TestCategoryAttributeStruct
    {
        /// <summary>
        /// Gets the test categories
        /// </summary>
        public IList<string>? TestCategories;
    }
}
