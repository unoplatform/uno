using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Uno.UI.Tasks.BatchMerge
{
	public class BatchMergeXaml : CustomTask
	{
		[Required]
		public ITaskItem[] Pages { get; set; } = null!;

		[Required]
		public string MergedXamlFile { get; set; } = null!;

		[Required]
		public string TlogReadFilesOutputPath { get; set; } = null!;

		[Required]
		public string TlogWriteFilesOutputPath { get; set; } = null!;

		[Output]
		public string[] FilesWritten
		{
			get { return filesWritten.ToArray(); }
		}

		private List<string> filesWritten = new List<string>();

		public override bool Execute()
		{
			MergedDictionary mergedDictionary = MergedDictionary.CreateMergedDicionary();
			List<string> pages = new List<string>();

			if (Pages != null)
			{
				foreach (ITaskItem pageItem in Pages)
				{
					string page = pageItem.ItemSpec;
					if (File.Exists(page))
					{
						pages.Add(page);
					}
					else
					{
						LogError($"Can't find page {page}!");
					}
				}
			}

			if (HasLoggedErrors)
			{
				return false;
			}

			LogMessage($"Merging XAML files into {MergedXamlFile}...");

			foreach (string page in pages)
			{
				try
				{
					mergedDictionary.MergeContent(File.ReadAllText(page));
				}
				catch (Exception)
				{
					LogError($"Exception found when merging page {page}!");
					throw;
				}
			}

			mergedDictionary.FinalizeXaml();
			filesWritten.Add(Utils.RewriteFileIfNecessary(MergedXamlFile, mergedDictionary.ToString()));

			File.WriteAllLines(TlogReadFilesOutputPath, Pages.Select(page => page.ItemSpec));
			File.WriteAllLines(TlogWriteFilesOutputPath, FilesWritten);

			return !HasLoggedErrors;
		}
	}
}
