using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocGenerator.Walkers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DocGenerator.Documentation.Files
{
    public class WorkspaceProjectDocumentationFile : CSharpDocumentationFile
    {
        private readonly Document _document;

        public WorkspaceProjectDocumentationFile(Document document) 
            : base(new FileInfo(document.FilePath))
        {
            _document = document;
        }

        public override async Task SaveToDocumentationFolderAsync()
        {
            var ast = _document.GetSyntaxTreeAsync().Result;

            var walker = new DocumentationFileWalker();
            walker.Visit(ast.GetRoot());
            var blocks = walker.Blocks.OrderBy(b => b.LineNumber).ToList();
            if (blocks.Count <= 0) return;

            var mergedBlocks = MergeAdjacentCodeBlocks(blocks);
            var body = this.RenderBlocksToDocumentation(mergedBlocks);
            var docFile = this.CreateDocumentationLocation();

            await CleanDocumentAndWriteToFileAsync(body, docFile);
        }
    }
}