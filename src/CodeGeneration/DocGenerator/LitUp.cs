using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DocGenerator.Documentation.Files;
using Microsoft.CodeAnalysis.MSBuild;

namespace DocGenerator
{
	public static class LitUp
	{
		private static readonly string[] SkipFolders = { "Nest.Tests.Literate", "Debug", "Release" };

		public static IEnumerable<DocumentationFile> InputFiles(string path) =>
			from f in Directory.GetFiles(Program.InputDirPath, $"{path}", SearchOption.AllDirectories)
			let dir = new DirectoryInfo(f)
			where dir?.Parent != null && !SkipFolders.Contains(dir.Parent.Name)
			select DocumentationFile.Load(new FileInfo(f));

		public static IEnumerable<IEnumerable<DocumentationFile>> Input
		{
			get
			{
			    var workspace = MSBuildWorkspace.Create();

                var project = workspace
                    .OpenProjectAsync(Path.Combine(Program.InputDirPath, "Tests.csproj")).Result;

			    yield return project.Documents
			        .Where(d => Path.GetFileName(d.FilePath)
			            .EndsWith(".doc.cs", StringComparison.OrdinalIgnoreCase))
			        .Select(d => new WorkspaceProjectDocumentationFile(workspace, d));

                yield return project.Documents
                    .Where(d => Path.GetFileName(d.FilePath)
                        .EndsWith("UsageTests.cs", StringComparison.OrdinalIgnoreCase))
                    .Select(d => new WorkspaceProjectDocumentationFile(workspace, d));

                yield return InputFiles("*.png");
				yield return InputFiles("*.gif");
				yield return InputFiles("*.jpg");
				// process asciidocs last as they may have generated
				// includes to other output asciidocs
				yield return InputFiles("*.asciidoc");
			}
		}

		public static void Go(string[] args)
		{
			foreach (var file in Input.SelectMany(s => s))
			{
				file.SaveToDocumentationFolder();
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
