//------------------------------------------------------------------------------
// <auto-generated />
// This file was automatically generated by the UpdateVendoredCode tool.
//------------------------------------------------------------------------------
#pragma warning disable CS0618, CS0649, CS1574, CS1580, CS1581, CS1584, CS1591, CS1573, CS8018, SYSLIB0011, SYSLIB0023, SYSLIB0032
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Datadog.Trace.Vendors.Microsoft.OpenApi.Expressions
{
    /// <summary>
    /// String literal with embedded expressions
    /// </summary>
    internal class CompositeExpression : RuntimeExpression
    {
        private readonly string template;
        private Regex expressionPattern = new(@"{(?<exp>\$[^}]*)");

        /// <summary>
        /// Expressions embedded into string literal
        /// </summary>
        public List<RuntimeExpression> ContainedExpressions = new();

        /// <summary>
        /// Create a composite expression from a string literal with an embedded expression
        /// </summary>
        /// <param name="expression"></param>
        public CompositeExpression(string expression)
        {
            template = expression;

            // Extract subexpressions and convert to RuntimeExpressions
            var matches = expressionPattern.Matches(expression);

            foreach (var item in matches.Cast<Match>())
            {
                var value = item.Groups["exp"].Captures.Cast<Capture>().First().Value;
                ContainedExpressions.Add(Build(value));
            }
        }

        /// <summary>
        /// Return original string literal with embedded expression
        /// </summary>
        public override string Expression => template;
    }
}
