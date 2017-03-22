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
        private readonly Compilation _compilation;

        public WorkspaceProjectDocumentationFile(Document document, Compilation compilation) 
            : base(new FileInfo(document.FilePath))
        {
            _document = document;
            _compilation = compilation;
        }

        public override async Task SaveToDocumentationFolderAsync()
        {
            var ast = await _document.GetSyntaxTreeAsync();

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