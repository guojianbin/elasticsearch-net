using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DocGenerator.Documentation.Blocks
{
    public class CSharpBlock : CodeBlock
    {
        private static readonly Regex Callout = new Regex(@"//[ \t]*(?<callout>\<\d+\>)[ \t]*(?<text>\S.*)", RegexOptions.Compiled);
        private static readonly Regex CalloutReplacer = new Regex(@"//[ \t]*\<(\d+)\>.*", RegexOptions.Compiled);

        private List<string> CallOuts { get; } = new List<string>();

        public CSharpBlock(SyntaxNode node, int depth, string memberName = null)
            : base(node.WithoutLeadingTrivia().ToFullString(),
                node.StartingLine(),
                node.IsKind(SyntaxKind.ClassDeclaration) ? depth : depth + 2,
                "csharp",
                memberName)
        {
        }

        public void AddNode(SyntaxNode node) => Lines.Add(node.WithLeadingEndOfLineTrivia().ToFullString());

        public override string ToAsciiDoc()
        {
            var builder = new StringBuilder();

            // method is used to reorder elements in GeneratedAsciidocVisitor
            builder.AppendLine(!string.IsNullOrEmpty(MemberName)
                ? $"[source, {Language.ToLowerInvariant()}, method=\"{MemberName.ToLowerInvariant()}\"]"
                : $"[source, {Language.ToLowerInvariant()}]");

            builder.AppendLine("----");

            var code = ExtractCallOutsFromCode(Value);

            code = code.RemoveNumberOfLeadingTabsOrSpacesAfterNewline(Depth);
            builder.AppendLine(code);

            builder.AppendLine("----");
            foreach (var callOut in CallOuts)
            {
                builder.AppendLine(callOut);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Extracts the call outs from code. The callout comment is defined inline within
        /// source code, but needs to be extracted and placed after the source block delimiter
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        private string ExtractCallOutsFromCode(string value)
        {
            var matches = Callout.Matches(value);
            var callouts = new List<string>();

            foreach (Match match in matches)
            {
                callouts.Add($"{match.Groups["callout"].Value} {match.Groups["text"].Value}");
            }

            if (callouts.Any())
            {
                value = CalloutReplacer.Replace(value, "//<$1>");
                CallOuts.AddRange(callouts);
            }

            return value.Trim();
        }
    }
}