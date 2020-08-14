using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;
using Mono.Options;
using Newtonsoft.Json;
using NUnit.Engine.Services;
using Uno.UI.TestComparer;
using Uno.UI.TestComparer.Comparer;

namespace Umbrella.UI.TestComparer
{
	class Program
	{
		static async Task Main(string[] args)
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

				ProcessFiles(basePath, basePath, null, "", "ios", "0");
				ProcessFiles(basePath, basePath, null, "", "Android", "0");
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
				var projectName = "";			// System.TeamProject
				var serverUri = "";					// System.TeamFoundationCollectionUri
				var currentBuild = 0;           // Build.BuildId

				var githubPAT = "";
				var sourceRepository = "";
				var githubPRid = "";

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

					//
					// GitHub PR comments related
					//
					{ "github-pat=", s => githubPAT = s },
					{ "source-repository=", s => sourceRepository = s },
					{ "github-pr-id=", s => githubPRid = s  }
				};

				var list = p.Parse(args);

				var targetBranch = !string.IsNullOrEmpty(targetBranchParam) && targetBranchParam != "$(System.PullRequest.TargetBranch)" ? targetBranchParam : sourceBranch;      	

				var downloader = new AzureDevopsDownloader(pat, serverUri);
				var artifacts = await downloader.DownloadArtifacts(basePath, projectName, definitionName, artifactName, sourceBranch, targetBranch, currentBuild, runLimit);

				var artifactsBasePath = Path.Combine(basePath, "artifacts");
				var results = new List<CompareResult>();

				foreach (var platform in GetValidPlatforms(artifactsBasePath))
				{
					var result = ProcessFiles(basePath, artifactsBasePath, artifacts, artifactInnerBasePath, platform, currentBuild.ToString());
					results.Add(result);
				}

				await TryPublishPRComments(results, githubPAT, sourceRepository, githubPRid, currentBuild);
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

		private static IEnumerable<string> GetValidPlatforms(string artifactsBasePath)
		{
			IEnumerable<string> GetAllPlatforms()
			{
				foreach (var toplevel in Directory.GetDirectories(artifactsBasePath, "*", SearchOption.TopDirectoryOnly))
				{
					foreach(var platform in Directory.GetDirectories(Path.Combine(toplevel, "uitests-results\\screenshots")))
					{
						yield return Path.GetFileName(platform);
					}
				}
			}

			return GetAllPlatforms().Distinct();
		}

		private static async Task TryPublishPRComments(List<CompareResult> results, string githubPAT, string sourceRepository, string githubPRid, int currentBuild)
		{
			var comment = await BuildPRComment(results, currentBuild);

			if (!int.TryParse(githubPRid, out _))
			{
				Console.WriteLine($"No valid GitHub PR Id found, no PR comment will be posted.");
				Console.WriteLine(comment);
				return;
			}

			if (string.IsNullOrEmpty(githubPAT.Trim()) || githubPAT.StartsWith("$("))
			{
				Console.WriteLine($"No GitHub PAT, no PR comment will be posted.");
				Console.WriteLine(comment);
				return;
			}

			await GitHubClient.PostPRCommentsAsync(githubPAT, sourceRepository, githubPRid, comment);
		}

		private static async Task<string> BuildPRComment(List<CompareResult> results, int currentBuild)
		{
			var comment = new StringBuilder();
			var hasErrors = results.Any(r => r.TotalTests - r.UnchangedTests != 0);

			if (hasErrors)
			{
				var summaryQuery =
					from result in results
					let lastChangedTests = result.Tests.Where(t => t.ResultRun.LastOrDefault()?.HasChanged ?? false).ToList()
					select $"`{result.Platform}`: **{lastChangedTests.Count}**";

				var summary = string.Join(", ", summaryQuery);

				comment.AppendLine($"The [build {currentBuild}](https://dev.azure.com/uno-platform/Uno%20Platform/_build/results?buildId={currentBuild}) found UI Test snapshots differences: {summary}\r\n");
				comment.AppendLine("<details>");
				comment.AppendLine($"<summary>Details</summary>\r\n");
				comment.AppendLine("");

				foreach (var result in results)
				{
					var lastChangedTests = result.Tests.Where(t => t.ResultRun.LastOrDefault()?.HasChanged ?? false).ToList();

					comment.AppendLine($"* `{result.Platform}`: **{lastChangedTests.Count}** changed over {result.TotalTests}\r\n");

					if (lastChangedTests.Any())
					{

						comment.AppendLine("  <details>");
						comment.AppendLine("  <summary>🚨🚨 Comparison Details (first 20) 🚨🚨</summary>");
						comment.AppendLine("");

						foreach (var test in lastChangedTests.Take(20))
						{
							comment.AppendLine($"  - `{Path.GetFileNameWithoutExtension(test.TestName)}`");
						}

						comment.AppendLine("  </details>");
						comment.AppendLine("");
					}
				}

				comment.AppendLine("</details>");
			}
			else
			{
				comment.Append($"The build {currentBuild} did not find any UI Test snapshots differences.");
			}

			return comment.ToString();
		}

		private static CompareResult ProcessFiles(string basePath, string artifactsBasePath, string[] artifacts, string artifactsInnerBasePath, string platform, string buildId)
		{
			var result = new TestFilesComparer(basePath, artifactsBasePath, artifactsInnerBasePath, platform).Compare(artifacts);

			GenerateHTMLResults(basePath, platform, result);
			GenerateNUnitTestResults(basePath, platform, result, buildId);

			return result;
		}

		private static void GenerateNUnitTestResults(string basePath, string platform, CompareResult compareResult, string buildId)
		{
			var resultsId = $"{DateTime.Now:yyyyMMdd-hhmmss}";
			var resultsFilePath = Path.Combine(basePath, $"Results-{platform}-{resultsId}.xml");

			var success = !compareResult.Tests.Any(t => t.ResultRun.LastOrDefault()?.HasChanged ?? false);
			var successCount = compareResult.Tests.Count(t => t.ResultRun.LastOrDefault()?.HasChanged ?? false);

			var doc = new XmlDocument();
			var rootNode = doc.CreateElement("test-run");
			doc.AppendChild(rootNode);
			rootNode.SetAttribute("id", buildId);
			rootNode.SetAttribute("name", resultsId);
			rootNode.SetAttribute("testcasecount", compareResult.TotalTests.ToString());
			rootNode.SetAttribute("result", success ? "Passed" : "Failed");
			rootNode.SetAttribute("time", "0");
			rootNode.SetAttribute("total", compareResult.TotalTests.ToString());
			rootNode.SetAttribute("errors", (compareResult.TotalTests - compareResult.UnchangedTests).ToString());
			rootNode.SetAttribute("passed", successCount.ToString());
			rootNode.SetAttribute("failed", "0");
			rootNode.SetAttribute("inconclusive", "0");
			rootNode.SetAttribute("skipped", "0");
			rootNode.SetAttribute("asserts", "0");

			var now = DateTimeOffset.Now;
			rootNode.SetAttribute("run-date", now.ToString("yyyy-MM-dd"));
			rootNode.SetAttribute("start-time", now.ToString("HH:mm:ss"));
			rootNode.SetAttribute("end-time", now.ToString("HH:mm:ss"));

			var testSuiteAssemblyNode = doc.CreateElement("test-suite");
			rootNode.AppendChild(testSuiteAssemblyNode);
			testSuiteAssemblyNode.SetAttribute("type", "Assembly");
			testSuiteAssemblyNode.SetAttribute("name", platform);

			var environmentNode = doc.CreateElement("environment");
			testSuiteAssemblyNode.AppendChild(environmentNode);
			environmentNode.SetAttribute("machine-name", Environment.MachineName);
			environmentNode.SetAttribute("platform", platform);

			var testSuiteFixtureNode = doc.CreateElement("test-suite");
			testSuiteAssemblyNode.AppendChild(testSuiteFixtureNode);


			testSuiteFixtureNode.SetAttribute("type", "TestFixture");
			testSuiteFixtureNode.SetAttribute("name", platform + "-" + resultsId);
			testSuiteFixtureNode.SetAttribute("executed", "true");

			testSuiteFixtureNode.SetAttribute("testcasecount", compareResult.TotalTests.ToString());
			testSuiteFixtureNode.SetAttribute("result", success ? "Passed" : "Failed");
			testSuiteFixtureNode.SetAttribute("time", "0");
			testSuiteFixtureNode.SetAttribute("total", compareResult.TotalTests.ToString());
			testSuiteFixtureNode.SetAttribute("errors", (compareResult.TotalTests - compareResult.UnchangedTests).ToString());
			testSuiteFixtureNode.SetAttribute("passed", successCount.ToString());
			testSuiteFixtureNode.SetAttribute("failed", "0");
			testSuiteFixtureNode.SetAttribute("inconclusive", "0");
			testSuiteFixtureNode.SetAttribute("skipped", "0");
			testSuiteFixtureNode.SetAttribute("asserts", "0");

			foreach (var run in compareResult.Tests)
			{
				var testCaseNode = doc.CreateElement("test-case");
				testSuiteFixtureNode.AppendChild(testCaseNode);

				var lastTestRun = run.ResultRun.LastOrDefault();

				testCaseNode.SetAttribute("name", platform + "-" + SanitizeTestName(run.TestName));
				testCaseNode.SetAttribute("fullname", platform + "-" + SanitizeTestName(run.TestName));
				testCaseNode.SetAttribute("duration", "0");
				testCaseNode.SetAttribute("time", "0");

				var testRunSuccess = !(lastTestRun?.HasChanged ?? false);
				testCaseNode.SetAttribute("result", testRunSuccess ? "Passed" : "Failed");

				if (lastTestRun != null)
				{
					if (!testRunSuccess)
					{
						var failureNode = doc.CreateElement("failure");
						testCaseNode.AppendChild(failureNode);

						var messageNode = doc.CreateElement("message");
						failureNode.AppendChild(messageNode);

						messageNode.InnerText = $"Results are different";
					}

					var attachmentsNode = doc.CreateElement("attachments");
					testCaseNode.AppendChild(attachmentsNode);

					AddAttachment(doc, attachmentsNode, lastTestRun.FilePath, "Result output");

					if (!testRunSuccess)
					{
						AddAttachment(doc, attachmentsNode, lastTestRun.DiffResultImage, "Image diff");

						var previousRun = run.ResultRun.ElementAtOrDefault(run.ResultRun.Count - 2);
						AddAttachment(doc, attachmentsNode, previousRun.FilePath, "Previous result output");
					}
				}
			}

			using (var file = XmlWriter.Create(File.OpenWrite(resultsFilePath), new XmlWriterSettings { Indent = true } ))
			{
				doc.WriteTo(file);
			}
		}

		private static string SanitizeTestName(string testName)
			=> testName.Replace(" ", "_");

		private static void AddAttachment(XmlDocument doc, XmlElement attachmentsNode, string filePath, string description)
		{
			var attachmentNode = doc.CreateElement("attachment");
			attachmentsNode.AppendChild(attachmentNode);

			var filePathNode = doc.CreateElement("filePath");
			attachmentNode.AppendChild(filePathNode);
			filePathNode.InnerText = filePath;

			var descriptionNode = doc.CreateElement("description");
			attachmentNode.AppendChild(descriptionNode);
			descriptionNode.InnerText = description;
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
