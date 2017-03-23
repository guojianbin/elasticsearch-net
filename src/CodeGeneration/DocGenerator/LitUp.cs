using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocGenerator.Documentation.Files;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace DocGenerator
{
	public static class LitUp
	{
		private static readonly string[] SkipFolders = { "Debug", "Release" };

		public static IEnumerable<DocumentationFile> InputFiles(string path) =>
			from f in Directory.GetFiles(Program.InputDirPath, $"{path}", SearchOption.AllDirectories)
			let dir = new DirectoryInfo(f)
			where dir?.Parent != null && !SkipFolders.Contains(dir.Parent.Name)
			select DocumentationFile.Load(new FileInfo(f));

		public static IEnumerable<IEnumerable<DocumentationFile>> GetDocumentFiles(Project project, Compilation compilation)
		{
            yield return project.Documents
               .Where(d => d.Name.EndsWith(".doc.cs", StringComparison.OrdinalIgnoreCase))
               .Select(d => new CSharpDocumentationFile(d, compilation));

            yield return project.Documents
                .Where(d => d.Name.EndsWith("UsageTests.cs", StringComparison.OrdinalIgnoreCase))
                .Select(d => new CSharpDocumentationFile(d, compilation));

            yield return InputFiles("*.png");
			yield return InputFiles("*.gif");
			yield return InputFiles("*.jpg");
			// process asciidocs last as they may have generated
			// includes to other output asciidocs
			yield return InputFiles("*.asciidoc");
	    }

		public static async Task GoAsync(string[] args)
		{
            var workspace = MSBuildWorkspace.Create();
            var project = await workspace.OpenProjectAsync(Path.Combine(Program.InputDirPath, "Tests.csproj"));

            // TODO: this throws OutOfMemory occasionally...
		    Compilation compilation = null; //await project.GetCompilationAsync();

            foreach (var file in GetDocumentFiles(project, compilation).SelectMany(s => s))
			{
				await file.SaveToDocumentationFolderAsync();
			}

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Documentation generated.");
			Console.ResetColor();

			if (Debugger.IsAttached)
			{
				Console.WriteLine("Press any key to continue...");
				Console.ReadKey();
			}
		}
	}
}
