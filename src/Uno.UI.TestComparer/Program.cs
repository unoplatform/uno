using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Mono.Options;
using Uno.UI.TestComparer;
using Uno.UI.TestComparer.Comparer;

namespace Umbrella.UI.TestComparer
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args[0] == "appcenter")
			{
				var basePath = args[1];
				var runLimit = int.Parse(args[2]);
				var appCenterApiKey = args[3];

				var p = new OptionSet() {
					{ "base-path=", s => basePath = s },
					{ "appCenterApiKey=", s => appCenterApiKey = s },
					{ "runLimit=", s => runLimit = int.Parse(s) },
				};

				var list = p.Parse(args);

				new AppCenterTestsDownloader(appCenterApiKey).Download(appCenterApiKey, basePath, runLimit).Wait();

				ProcessFiles(basePath, basePath, "", "ios");
				ProcessFiles(basePath, basePath, "", "Android");
			}
			else if (args[0] == "azdo")
			{
				var basePath = "";
				var runLimit = 0;
				var pat = "";
				var sourceBranch = "";
				var targetBranchParam = "";
				var artifactName = ""; 
				var artifactInnerBasePath = ""; // base path inside the artifact archive
				var definitionName = "";		// Build.DefinitionName
				var projectName = "";      // System.TeamProject
				var serverUri = "";        // System.TeamFoundationCollectionUri
				var currentBuild = 0;			// Build.BuildId

				var p = new OptionSet() {
					{ "base-path=", s => basePath = s },
					{ "pat=", s => pat = s },
					{ "run-limit=", s => runLimit = int.Parse(s) },
					{ "source-branch=", s => sourceBranch = s },   // Build.SourceBranchName	
					{ "target-branch=", s => targetBranchParam = s },   // System.PullRequest.TargetBranch
					{ "artifact-name=", s => artifactName = s },
					{ "artifact-inner-path=", s => artifactInnerBasePath = s },
					{ "definition-name=", s => definitionName = s },
					{ "project-name=", s => projectName = s },
					{ "server-uri=", s => serverUri = s },
					{ "current-build=", s => currentBuild = int.Parse(s) },
				};

				var list = p.Parse(args);

				var targetBranch = !string.IsNullOrEmpty(targetBranchParam) && targetBranchParam != "$(System.PullRequest.TargetBranch)" ? targetBranchParam : sourceBranch;      	

				var downloader = new AzureDevopsDownloader(pat, serverUri);
				downloader.DownloadArtifacts(basePath, projectName, definitionName, artifactName, sourceBranch, targetBranch, currentBuild, runLimit).Wait();

				var artifactsBasePath = Path.Combine(basePath, "artifacts");
				ProcessFiles(basePath, artifactsBasePath, artifactInnerBasePath, "wasm");
				ProcessFiles(basePath, artifactsBasePath, artifactInnerBasePath, "wasm-automated");
				ProcessFiles(basePath, artifactsBasePath, artifactInnerBasePath, "android");
			}
			else if (args[0] == "compare")
			{
				var leftPath = args[0];
				var rightPath = args[1];
				var outputPath = args[2];

				var q = from leftFilePath in Directory.EnumerateFiles(leftPath, "*.png")
						let leftFileName = Path.GetFileName(leftFilePath)
						join rightFilePath in Directory.EnumerateFiles(rightPath, "*.png") on leftFileName equals Path.GetFileName(rightFilePath) into gj
						from pair in gj.DefaultIfEmpty()
						select new
						{
							LeftPath = leftFilePath,
							RightPath = pair
						};

				using (var file = new StreamWriter(outputPath))
				{
					file.Write("<html><body>");
					file.Write("<table>");

					foreach (var pair in q)
					{
						var areValidFiles = pair.LeftPath != null && pair.RightPath != null;
						var areEqual = areValidFiles ?
							File.ReadAllBytes(pair.LeftPath).SequenceEqual(File.ReadAllBytes(pair.RightPath))
							: false;

						if (!areEqual)
						{
							file.Write("<tr>");
							file.Write("<td>");
							file.Write($"<img src=\"file:///{pair.LeftPath}\" width=400 height=400 />");
							file.Write("</td>");
							file.Write("<td>");
							file.Write($"<img src=\"file:///{pair.RightPath}\"  width=400 height=400 />");
							file.Write("</td>");
							file.Write("</tr>");
						}
					}
					file.Write("</table>");
					file.Write("</body></html>");
				}
			}
		}

		private static void ProcessFiles(string basePath, string artifactsBasePath, string artifactsInnerBasePath, string platform)
		{
			var result = new TestFilesComparer(basePath, artifactsBasePath, artifactsInnerBasePath, platform).Compare();

			GenerateHTMLResults(basePath, platform, result);
		}

		private static void GenerateHTMLResults(string basePath, string platform, CompareResult result)
		{
			string path = basePath;
			var resultsId = $"{DateTime.Now:yyyyMMdd-hhmmss}";
			string diffPath = Path.Combine(basePath, $"Results-{platform}-{resultsId}");
			string resultsFilePath = Path.Combine(basePath, $"Results-{platform}-{resultsId}.html");

			Directory.CreateDirectory(path);
			Directory.CreateDirectory(diffPath);

			var sb = new StringBuilder();

			sb.AppendLine("<html><body>");

			sb.AppendLine("<p>How to read this table:</p>");
			sb.AppendLine("<ul>");
			sb.AppendLine("<li>This tool compares the binary content of successive runs screenshots of the same test output</li>");
			sb.AppendLine("<li>Each line represents a test result screen shot.</li>");
			sb.AppendLine("<li>Each cell number represent an unique image ID.</li>");
			sb.AppendLine("<li>A red cell means the image has changed from the previous run (not that the results are incorrect).</li>");
			sb.AppendLine("<li>Subtle changes can be visualized using a XOR filtering between images</li>");
			sb.AppendLine("</ul>");

			sb.AppendLine("<ul>");
			foreach (var folder in result.Folders)
			{
				sb.AppendLine($"<li>{folder.index}: {folder.path}</li>");
			}
			sb.AppendLine("</ul>");

			sb.AppendLine("<table>");

			sb.AppendLine("<tr><td/>");
			foreach (var folder in result.Folders)
			{
				sb.AppendLine($"<td>{folder.index}</td>");
			}
			sb.AppendLine("</tr>");

			var changedList = new List<string>();
			foreach (var testFile in result.Tests)
			{
				if (testFile.HasChanged)
				{
					sb.AppendLine($"<tr rowspan=\"3\"><td>{testFile.TestName}</td></tr>");

					for (int platformIndex = 0; platformIndex < 1; platformIndex++)
					{
						sb.AppendLine("<tr>");
						sb.AppendLine($"<td></td>");


						for (int folderIndex = 0; folderIndex < testFile.ResultRun.Count; folderIndex++)
						{
							var resultRun = testFile.ResultRun[folderIndex];

							var hasChanged = resultRun.HasChanged;
							var color = resultRun.ImageId == -1 ? "#555" : hasChanged ? "#f00" : "#0f0";

							sb.AppendLine($"<td bgcolor=\"{color}\" width=20 height=20>");

							if (resultRun.ImageId != -1)
							{
								var relativePath = resultRun.FilePath.Replace(basePath, "").Replace("\\", "/").TrimStart('/');
								sb.AppendLine($"<a href=\"{relativePath}\">{resultRun.ImageId}</a><!--{resultRun.ImageSha}-->");

								var previousRun = testFile.ResultRun.ElementAtOrDefault(folderIndex - 1);
								if (resultRun.HasChanged && resultRun.DiffResultImage != null)
								{
									var diffRelativePath = resultRun.DiffResultImage.Replace(basePath, "").Replace("\\", "/").TrimStart('/');
									sb.AppendLine($"<a href=\"{diffRelativePath}\">D</a>");
								}
								else
								{
									sb.AppendLine($"D");
								}
							}
							sb.AppendLine("</td>");
						}

						sb.AppendLine("</tr>");
					}
				}
			}

			sb.AppendLine("</table>");
			sb.AppendLine($"{result.UnchangedTests} samples unchanged, {result.TotalTests} files total.");
			sb.AppendLine("</body></html>");

			File.WriteAllText(resultsFilePath, sb.ToString());

			Console.WriteLine($"{platform}: {result.UnchangedTests} samples unchanged, {result.TotalTests} files total. Changed files:");

			foreach (var changedFile in changedList)
			{
				Console.WriteLine($"\t- {changedFile}");
			}
		}
	}
}
