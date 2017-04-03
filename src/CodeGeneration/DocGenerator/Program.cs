using System;
using System.IO;

namespace DocGenerator
{
	public static class Program
	{
		static Program()
		{
			var currentDirectory = new DirectoryInfo(Directory.GetCurrentDirectory());
			if (currentDirectory.Name == "DocGenerator" && currentDirectory.Parent.Name == "CodeGeneration")
			{
				InputDirPath = @"..\..\";
				OutputDirPath = @"..\..\..\docs";
			}
			else
			{
				InputDirPath = @"..\..\..\..\..\src";
				OutputDirPath = @"..\..\..\..\..\docs";
			}
		}

		public static string InputDirPath { get; }

		public static string OutputDirPath { get; }

		static void Main(string[] args)
		{
		    try
		    {
                LitUp.GoAsync(args).Wait();
            }
		    catch (AggregateException ae)
		    {
                throw ae.InnerException ?? ae;
            }
		}
	}
}
