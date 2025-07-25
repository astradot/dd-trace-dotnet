// <copyright file="EnumerateAssemblyReferencesTest.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System.Threading.Tasks;
using Datadog.Trace.TestHelpers;
using Xunit;
using Xunit.Abstractions;

namespace Datadog.Trace.ClrProfiler.IntegrationTests.SmokeTests
{
    public class EnumerateAssemblyReferencesTest : SmokeTestBase
    {
        public EnumerateAssemblyReferencesTest(ITestOutputHelper output)
            : base(output, "EnumerateAssemblyReferences")
        {
        }

        [SkippableFact]
        [Trait("Category", "Smoke")]
        [Flaky("This test often crashes with a shutdown bug which we haven't been able to track down yet")]
        public async Task NoExceptions()
        {
            await CheckForSmoke(shouldDeserializeTraces: false);
        }
    }
}
