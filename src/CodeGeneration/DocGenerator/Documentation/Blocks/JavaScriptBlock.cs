namespace DocGenerator.Documentation.Blocks
{
    public class JavaScriptBlock : CodeBlock
    {
        public JavaScriptBlock(string text, int startingLine, int depth, string memberName = null)
            : base(text, startingLine, depth, "javascript", memberName)
        {
        }
    }
}