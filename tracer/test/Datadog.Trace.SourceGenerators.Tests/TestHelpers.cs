// <copyright file="TestHelpers.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Datadog.Trace.SourceGenerators.Helpers;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Datadog.Trace.SourceGenerators.Tests
{
    internal class TestHelpers
    {
        public static (ImmutableArray<Diagnostic> Diagnostics, string Output) GetGeneratedOutput<T>(params string[] source)
            where T : IIncrementalGenerator, new()
        {
            var (diagnostics, trees) = GetGeneratedTrees<T, TrackingNames>(source);
            return (diagnostics, trees.LastOrDefault() ?? string.Empty);
        }

        public static (ImmutableArray<Diagnostic> Diagnostics, string[] Output) GetGeneratedTrees<TGenerator>(params string[] source)
            where TGenerator : IIncrementalGenerator, new()
            => GetGeneratedTrees<TGenerator, TrackingNames>(source);

        public static (ImmutableArray<Diagnostic> Diagnostics, string[] Output) GetGeneratedTrees<TGenerator>(string[] source, bool assertOutput)
            where TGenerator : IIncrementalGenerator, new()
            => GetGeneratedTrees<TGenerator, TrackingNames>(source, assertOutput);

        public static (ImmutableArray<Diagnostic> Diagnostics, string[] Output) GetGeneratedTrees<TGenerator, TTrackingNames>(params string[] sources)
            where TGenerator : IIncrementalGenerator, new()
            => GetGeneratedTrees<TGenerator, TTrackingNames>(sources, assertOutput: true);

        public static (ImmutableArray<Diagnostic> Diagnostics, string[] Output) GetGeneratedTrees<TGenerator, TTrackingNames>(string[] sources, bool assertOutput)
            where TGenerator : IIncrementalGenerator, new()
        {
            // get all the const string fields
            var trackingNames = typeof(TTrackingNames)
                               .GetFields()
                               .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                               .Select(x => (string)x.GetRawConstantValue())
                               .Where(x => !string.IsNullOrEmpty(x))
                               .ToArray();

            return GetGeneratedTrees<TGenerator>(sources, trackingNames, assertOutput);
        }

        public static (ImmutableArray<Diagnostic> Diagnostics, string[] Output) GetGeneratedTrees<T>(string[] source, string[] stages, bool assertOutput = true)
            where T : IIncrementalGenerator, new()
        {
            IEnumerable<SyntaxTree> syntaxTrees = source.Select(static x => CSharpSyntaxTree.ParseText(x));
            var references = AppDomain.CurrentDomain.GetAssemblies()
                                      .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
                                      .Select(_ => MetadataReference.CreateFromFile(_.Location))
                                      .Concat(new[] { MetadataReference.CreateFromFile(typeof(T).Assembly.Location) });

            CSharpCompilation compilation = CSharpCompilation.Create(
                "Datadog.Trace.Generated",
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            GeneratorDriverRunResult runResult = RunGeneratorAndAssertOutput<T>(compilation, stages, assertOutput);

            return (runResult.Diagnostics, runResult.GeneratedTrees.Select(x => x.ToString()).ToArray());
        }

        private static GeneratorDriverRunResult RunGeneratorAndAssertOutput<T>(CSharpCompilation compilation, string[] trackingNames, bool assertOutput = true)
            where T : IIncrementalGenerator, new()
        {
            ISourceGenerator generator = new T().AsSourceGenerator();

            var opts = new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true);

            GeneratorDriver driver = CSharpGeneratorDriver.Create([generator], driverOptions: opts);

            var clone = compilation.Clone();
            // Run twice, once with a clone of the compilation
            // Note that we store the returned drive value, as it contains cached previous outputs
            driver = driver.RunGenerators(compilation);
            GeneratorDriverRunResult runResult = driver.GetRunResult();

            if (assertOutput)
            {
                // Run with a clone of the compilation
                GeneratorDriverRunResult runResult2 = driver
                                                     .RunGenerators(clone)
                                                     .GetRunResult();

                AssertRunsEqual(runResult, runResult2, trackingNames);

                // verify the second run only generated cached source outputs
                runResult2.Results[0]
                          .TrackedOutputSteps
                          .SelectMany(x => x.Value) // step executions
                          .SelectMany(x => x.Outputs) // execution results
                          .Should()
                          .OnlyContain(x => x.Reason == IncrementalStepRunReason.Cached);
            }

            return runResult;
        }

        private static void AssertRunsEqual(GeneratorDriverRunResult runResult1, GeneratorDriverRunResult runResult2, string[] trackingNames)
        {
            // We're given all the tracking names, but not all the stages have necessarily executed so filter
            Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> trackedSteps1 = GetTrackedSteps(runResult1, trackingNames);
            Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> trackedSteps2 = GetTrackedSteps(runResult2, trackingNames);

            // These should be the same
            trackedSteps1.Should()
                         .NotBeEmpty()
                         .And.HaveSameCount(trackedSteps2)
                         .And.ContainKeys(trackedSteps2.Keys);

            foreach (var trackedStep in trackedSteps1)
            {
                var trackingName = trackedStep.Key;
                var runSteps1 = trackedStep.Value;
                var runSteps2 = trackedSteps2[trackingName];
                AssertEqual(runSteps1, runSteps2, trackingName);
            }

            return;

            static Dictionary<string, ImmutableArray<IncrementalGeneratorRunStep>> GetTrackedSteps(
                GeneratorDriverRunResult runResult, string[] trackingNames) =>
                runResult.Results[0]
                         .TrackedSteps
                         .Where(step => trackingNames.Contains(step.Key))
                         .ToDictionary(x => x.Key, x => x.Value);
        }

        private static void AssertEqual(
            ImmutableArray<IncrementalGeneratorRunStep> runSteps1,
            ImmutableArray<IncrementalGeneratorRunStep> runSteps2,
            string stepName)
        {
            runSteps1.Should().HaveSameCount(runSteps2);

            for (var i = 0; i < runSteps1.Length; i++)
            {
                var runStep1 = runSteps1[i];
                var runStep2 = runSteps2[i];

                // The outputs should be equal between different runs
                IEnumerable<object> outputs1 = runStep1.Outputs.Select(x => x.Value);
                IEnumerable<object> outputs2 = runStep2.Outputs.Select(x => x.Value);

                outputs1.Should()
                        .Equal(outputs2, $"because {stepName} should produce cacheable outputs");

                // Therefore, on the second run the results should always be cached or unchanged!
                // - Unchanged is when the _input_ has changed, but the output hasn't
                // - Cached is when the the input has not changed, so the cached output is used
                runStep2.Outputs.Should()
                        .OnlyContain(
                             x => x.Reason == IncrementalStepRunReason.Cached || x.Reason == IncrementalStepRunReason.Unchanged,
                             $"{stepName} expected to have reason {IncrementalStepRunReason.Cached} or {IncrementalStepRunReason.Unchanged}");

                // Make sure we're not using anything we shouldn't
                // TODO this causes _weird_ stack overflow issues with EquatableArray
                // AssertObjectGraph(runStep1, stepName);
                // AssertObjectGraph(runStep2, stepName);
            }

#pragma warning disable CS8321 // Local function is declared but never used
            static void AssertObjectGraph(IncrementalGeneratorRunStep runStep, string stepName)
#pragma warning restore CS8321 // Local function is declared but never used
            {
                var because = $"{stepName} shouldn't contain banned symbols";
                var visited = new HashSet<object>();
                var queue = new Queue<object>();

                // Populate queue
                foreach (var (obj, _) in runStep.Outputs)
                {
                    Visit(obj);
                }

                // drain queue
                while (queue.Count > 0)
                {
                    Visit(queue.Dequeue());
                }

                void Visit(object node)
                {
                    if (node is null || !visited.Add(node))
                    {
                        return;
                    }

                    node.Should()
                       .NotBeOfType<Compilation>(because)
                       .And.NotBeOfType<ISymbol>(because)
                       .And.NotBeOfType<SyntaxNode>(because);

                    Type type = node.GetType();
                    if (type.IsPrimitive || type.IsEnum || type == typeof(string))
                    {
                        return;
                    }

                    if (node is IEnumerable collection and not string)
                    {
                        foreach (object element in collection)
                        {
                            queue.Enqueue(element);
                        }

                        return;
                    }

                    foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                    {
                        object fieldValue = field.GetValue(node);
                        queue.Enqueue(fieldValue);
                    }
                }
            }
        }
    }
}
