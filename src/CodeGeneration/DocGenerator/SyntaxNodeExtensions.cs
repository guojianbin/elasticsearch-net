using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DocGenerator
{
	public static class SyntaxNodeExtensions
	{
        private static readonly Regex SingleLineHideComment = new Regex(@"\/\/\s*hide", RegexOptions.Compiled);
        private static readonly Regex SingleLineJsonComment = new Regex(@"\/\/\s*json", RegexOptions.Compiled);

        /// <summary>
        /// Determines if the node should be hidden i.e. not included in the documentation,
        /// based on the precedence of a //hide single line comment
        /// </summary>
        public static bool ShouldBeHidden(this SyntaxNode node) => 
            node.HasLeadingTrivia && 
            SingleLineHideComment.IsMatch(node.GetLeadingTrivia().ToFullString());

        /// <summary>
        /// Determines if the node should be json serialized based on the precedence of
        /// a //json single line comment
        /// </summary>
        public static bool ShouldBeConvertedToJson(this SyntaxNode node)
        {
            if (!node.HasLeadingTrivia)
                return false;

            var leadingTrivia = node.GetLeadingTrivia();

            var singleLineCommentIndex = leadingTrivia.IndexOf(SyntaxKind.SingleLineCommentTrivia);

            if (singleLineCommentIndex == -1)
                return false;

            if (!leadingTrivia
                .SkipWhile((l, i) => i < singleLineCommentIndex)
                .Any(l => l.IsKind(SyntaxKind.EndOfLineTrivia) || l.IsKind(SyntaxKind.WhitespaceTrivia)))
            {
                return false;
            }
            
            return SingleLineJsonComment.IsMatch(leadingTrivia.ElementAt(singleLineCommentIndex).ToFullString());
        }
	}
}