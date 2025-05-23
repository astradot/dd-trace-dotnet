// <copyright file="CodeOwners.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Datadog.Trace.Util;

namespace Datadog.Trace.Ci
{
    internal class CodeOwners
    {
        private readonly IGrouping<string?, Entry>[] _sections;

        public CodeOwners(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                ThrowHelper.ThrowArgumentNullException(nameof(filePath));
            }

            var entriesList = new List<Entry>();
            var sectionsList = new List<string>();
            string? currentSectionName = null;
            var atSpan = "@".AsSpan();
            foreach (var line in File.ReadLines(filePath))
            {
                if (string.IsNullOrEmpty(line) || line[0] == '#')
                {
                    continue;
                }

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSectionName = line.Substring(1, line.Length - 2);
                    var foundSectionName = sectionsList.FirstOrDefault(s => string.Equals(s, currentSectionName, StringComparison.OrdinalIgnoreCase));
                    if (foundSectionName is null)
                    {
                        sectionsList.Add(currentSectionName);
                    }

                    currentSectionName = foundSectionName ?? currentSectionName;
                    continue;
                }

                var finalLine = line;
                var ownersList = new List<string>();
                foreach (var currentTermSplitValue in line.SplitIntoSpans(' '))
                {
                    var currentTerm = currentTermSplitValue.AsSpan();
                    if (currentTerm.Length == 0)
                    {
                        continue;
                    }

                    // Teams and users handles starts with @
                    // Emails contains @
                    if (currentTerm[0] == '@' || currentTerm.Contains(atSpan, StringComparison.Ordinal))
                    {
                        ownersList.Add(currentTerm.ToString());
                        var pos = finalLine.AsSpan().IndexOf(currentTerm, StringComparison.Ordinal);
                        if (pos > 0)
                        {
                            finalLine = finalLine.Remove(pos, currentTerm.Length);
                        }
                    }
                }

                finalLine = finalLine.Trim();
                if (finalLine.Length == 0)
                {
                    continue;
                }

                entriesList.Add(new Entry(finalLine, ownersList.ToArray(), currentSectionName));
            }

            entriesList.Reverse();
            _sections = entriesList.GroupBy(i => i.Section).ToArray();
        }

        public Entry? Match(string value)
        {
            var lstEntries = new List<Entry>();
            foreach (var section in _sections)
            {
                foreach (var entry in section)
                {
                    var pattern = entry.Pattern;
                    var finalPattern = pattern;

                    bool includeAnythingBefore;
                    bool includeAnythingAfter;

                    if (pattern.StartsWith("/"))
                    {
                        includeAnythingBefore = false;
                    }
                    else
                    {
                        if (finalPattern.StartsWith("*"))
                        {
                            finalPattern = finalPattern.Substring(1);
                        }

                        includeAnythingBefore = true;
                    }

                    if (pattern.EndsWith("/"))
                    {
                        includeAnythingAfter = true;
                    }
                    else if (pattern.EndsWith("/*"))
                    {
                        includeAnythingAfter = true;
                        finalPattern = finalPattern.Substring(0, finalPattern.Length - 1);
                    }
                    else
                    {
                        includeAnythingAfter = false;
                    }

                    if (includeAnythingAfter)
                    {
                        var found = includeAnythingBefore ? value.Contains(finalPattern) : value.StartsWith(finalPattern);
                        if (!found)
                        {
                            continue;
                        }

                        if (!pattern.EndsWith("/*"))
                        {
                            lstEntries.Add(entry);
                            break;
                        }

                        var patternEnd = value.IndexOf(finalPattern, StringComparison.Ordinal);
                        if (patternEnd != -1)
                        {
                            patternEnd += finalPattern.Length;
                            var remainingString = value.Substring(patternEnd);
                            if (remainingString.IndexOf("/", StringComparison.Ordinal) == -1)
                            {
                                lstEntries.Add(entry);
                                break;
                            }
                        }
                    }
                    else
                    {
                        if (includeAnythingBefore)
                        {
                            if (value.EndsWith(finalPattern))
                            {
                                lstEntries.Add(entry);
                                break;
                            }
                        }
                        else if (value == finalPattern)
                        {
                            lstEntries.Add(entry);
                            break;
                        }
                    }
                }
            }

            return lstEntries.Count switch
            {
                0 => null,
                1 => lstEntries[0],
                _ => lstEntries.Aggregate((a, b) => new Entry($"{a.Pattern} | {b.Pattern}", a.Owners.Concat(b.Owners).ToArray(), $"{a.Section} | {b.Section}"))
            };
        }

        internal readonly struct Entry
        {
            public readonly string Pattern;
            public readonly IEnumerable<string> Owners;
            public readonly string? Section;

            public Entry(string pattern, IEnumerable<string> owners, string? section)
            {
                Pattern = pattern;
                Owners = owners;
                Section = section;
            }

            public string? GetOwnersString()
            {
                if (Owners is null || !Owners.Any())
                {
                    return null;
                }

                return "[\"" + string.Join("\",\"", Owners) + "\"]";
            }
        }
    }
}
