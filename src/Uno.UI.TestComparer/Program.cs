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

				ProcessFiles(args, basePath, basePath, "", "ios");
				ProcessFiles(args, basePath, basePath, "", "Android");
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
				ProcessFiles(args, basePath, artifactsBasePath, artifactInnerBasePath, "wasm");
				ProcessFiles(args, basePath, artifactsBasePath, artifactInnerBasePath, "wasm-automated");
				ProcessFiles(args, basePath, artifactsBasePath, artifactInnerBasePath, "android");
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

		private static void ProcessFiles(string[] args, string basePath, string artifactsBasePath, string artifactsInnerBasePath, string platform)
		{
			string path = basePath;
			var resultsId = $"{DateTime.Now:yyyyMMdd-hhmmss}";
			string diffPath = Path.Combine(basePath, $"Results-{platform}-{resultsId}");
			string resultsFilePath = Path.Combine(basePath, $"Results-{platform}-{resultsId}.html");

			Directory.CreateDirectory(path);
			Directory.CreateDirectory(diffPath);

			var q1 = from directory in Directory.EnumerateDirectories(artifactsBasePath)
					 let info = new DirectoryInfo(directory)
					 orderby info.CreationTime descending
					 select directory;

			q1 = q1.ToArray();

			var orderedDirectories = from directory in q1
									 orderby directory
									 select directory;

			var q = from directory in orderedDirectories.Select((v, i) => new { Index = i, Path = v })
					let files = from sample in EnumerateFiles(Path.Combine(directory.Path, artifactsInnerBasePath, platform), "*.png").AsParallel()
								select new { File = sample, Id = BuildSha1(sample) }
					select new
					{
						Path = directory.Path,
						Index = directory.Index,
						Files = files.ToArray(),
					};

			var allFolders = LogForeach(q, i => Console.WriteLine($"Processed {i.Path}")).ToArray();

			var allFiles = allFolders
				.SelectMany(folder => folder
					.Files.Select(file => Path.GetFileName(file.File))
				)
				.Distinct()
				.ToArray();

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
			foreach (var folder in allFolders)
			{
				sb.AppendLine($"<li>{folder.Index}: {folder.Path}</li>");
			}
			sb.AppendLine("</ul>");

			sb.AppendLine("<table>");

			sb.AppendLine("<tr><td/>");
			foreach (var folder in allFolders)
			{
				sb.AppendLine($"<td>{folder.Index}</td>");
			}
			sb.AppendLine("</tr>");

			var changedList = new List<string>();
			foreach (var testFile in allFiles)
			{
				var testFileIncrements = from folder in allFolders
										 orderby folder.Index ascending
										 select new
										 {
											 Folder = folder.Path,
											 FolderIndex = folder.Index,
											 Files = new[] {
												new { FileInfo = folder.Files.FirstOrDefault(f => Path.GetFileName(f.File) == testFile) }
											 }
										 };

				var increments =
					(
						from platformIndex in Enumerable.Range(0, 1)
						select testFileIncrements
							.Aggregate(
								new[] { new { FolderIndex = -1, Path = "", IdSha = "", Id = -1, CompareeId = -1 } },
								(a, f) =>
								{
									var platformFiles = f.Files[platformIndex];

									if (platformFiles?.FileInfo == null)
									{
										return a;
									}

									var otherMatch = a.Reverse().Where(i => i.IdSha != null).FirstOrDefault();
									if (platformFiles.FileInfo?.Id.sha != otherMatch?.IdSha)
									{
										return a
											.Concat(new[] { new { FolderIndex = f.FolderIndex, Path = platformFiles.FileInfo.File, IdSha = platformFiles.FileInfo?.Id.sha, Id = platformFiles.FileInfo?.Id.index ?? -1, CompareeId = otherMatch.Id } })
											.ToArray();
									}
									else
									{
										return a
											.Concat(new[] { new { FolderIndex = f.FolderIndex, Path = platformFiles.FileInfo.File, IdSha = (string)null, Id = platformFiles.FileInfo.Id.index, CompareeId = -1 } })
											.ToArray();
									}
								}
							)
					).ToArray();

				var hasChanges = increments.Any(i => i.Where(v => v.IdSha != null).Count() - 1 > 1);

				if (hasChanges)
				{
					sb.AppendLine($"<tr rowspan=\"3\"><td>{testFile}</td></tr>");

					for (int platformIndex = 0; platformIndex < 1; platformIndex++)
					{
						sb.AppendLine("<tr>");
						sb.AppendLine($"<td></td>");

						if (increments[platformIndex].Count() > 1)
						{
							var firstFolder = increments[platformIndex].Where(i => i.FolderIndex != -1).Min(i => i.FolderIndex);

							for (int folderIndex = 0; folderIndex < allFolders.Length; folderIndex++)
							{
								var folderInfo = increments[platformIndex].FirstOrDefault(inc => inc.FolderIndex == folderIndex);

								var hasChanged = folderIndex != firstFolder && folderInfo?.IdSha != null;
								var color = folderInfo == null ? "#555" : hasChanged ? "#f00" : "#0f0";

								sb.AppendLine($"<td bgcolor=\"{color}\" width=20 height=20>");
								if (folderInfo != null)
								{
									var relativePath = folderInfo.Path.Replace(basePath, "").Replace("\\", "/").TrimStart('/');
									sb.AppendLine($"<a href=\"{relativePath}\">{folderInfo.Id}</a><!--{folderInfo.IdSha}-->");

									var previousFolderInfo = increments[platformIndex].FirstOrDefault(inc => inc.FolderIndex == folderIndex - 1);
									if (hasChanged && previousFolderInfo != null)
									{
										var currentImage = DecodeImage(folderInfo.Path);
										var previousImage = DecodeImage(previousFolderInfo.Path);

										var diff = DiffImages(currentImage.pixels, previousImage.pixels);

										var diffFilePath = Path.Combine(diffPath, $"{folderInfo.Id}-{folderInfo.CompareeId}.png");
										WriteImage(diffFilePath, diff, currentImage.frame);

										var diffRelativePath = diffFilePath.Replace(basePath, "").Replace("\\", "/").TrimStart('/');
										sb.AppendLine($"<a href=\"{diffRelativePath}\">D</a>");
									}
									else
									{
										sb.AppendLine($"D");
									}
								}
								sb.AppendLine("</td>");
							}
						}

						sb.AppendLine("</tr>");
					}

					changedList.Add(testFile);
				}
			}

			var unchanged = allFiles.Length - changedList.Count;

			sb.AppendLine("</table>");
			sb.AppendLine($"{unchanged} samples unchanged, {allFiles.Length} files total.");
			sb.AppendLine("</body></html>");

			File.WriteAllText(resultsFilePath, sb.ToString());

			Console.WriteLine($"{platform}: {unchanged} samples unchanged, {allFiles.Length} files total. Changed files:");

			foreach (var changedFile in changedList)
			{
				Console.WriteLine($"\t- {changedFile}");
			}
		}

		private static void WriteImage(string diffPath, byte[] diff, BitmapFrame frameInfo)
		{
			using (var stream = new FileStream(diffPath, FileMode.Create))
			{
				var encoder = new PngBitmapEncoder();

				encoder.Interlace = PngInterlaceOption.On;

				var frame = BitmapSource.Create(
					pixelWidth: (int)frameInfo.Width,
					pixelHeight: (int)frameInfo.Height,
					dpiX: frameInfo.DpiX,
					dpiY: frameInfo.DpiY,
					pixelFormat: frameInfo.Format,
					palette: frameInfo.Palette,
					pixels: diff,
					stride: (int)(frameInfo.Width * 4)
				);

				encoder.Frames.Add(BitmapFrame.Create(frame));
				encoder.Save(stream);
			}
		}

		private static byte[] DiffImages(byte[] currentImage, byte[] previousImage)
		{
			var result = new byte[currentImage.Length];

			for (int i = 0; i < result.Length; i++)
			{
				result[i] = (byte)(currentImage[i] ^ previousImage[i]);
			}

			return result;
		}

		private static (BitmapFrame frame, byte[] pixels) DecodeImage(string path1)
		{
			Stream imageStreamSource = new FileStream(path1, FileMode.Open, FileAccess.Read, FileShare.Read);
			var decoder = new PngBitmapDecoder(imageStreamSource, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.Default);

			byte[] image = new byte[(int)(decoder.Frames[0].Width * decoder.Frames[0].Height * 4)];
			decoder.Frames[0].CopyPixels(image, (int)(decoder.Frames[0].Width * 4), 0);

			return (decoder.Frames[0], image);
		}

		private static IEnumerable<string> EnumerateFiles(string path, string pattern)
		{
			if (Directory.Exists(path))
			{
				return Directory.EnumerateFiles(path, pattern, SearchOption.AllDirectories);
			}
			else
			{
				return new string[0];
			}
		}

		private static IEnumerable<T> LogForeach<T>(IEnumerable<T> q, Action<T> action)
		{
			foreach (var item in q)
			{
				action(item);
				yield return item;
			}
		}

		static Dictionary<string, int> _fileHashesTable = new Dictionary<string, int>();

		private static (string sha, int index) BuildSha1(string sample)
		{
			// return "000";

			using (var sha1 = SHA1.Create())
			{
				using (var file = File.OpenRead(sample))
				{
					var data = sha1.ComputeHash(file);

					// Create a new Stringbuilder to collect the bytes
					// and create a string.
					var sBuilder = new StringBuilder();

					// Loop through each byte of the hashed data 
					// and format each one as a hexadecimal string.
					for (int i = 0; i < data.Length; i++)
					{
						sBuilder.Append(data[i].ToString("x2", CultureInfo.InvariantCulture));
					}

					var sha = sBuilder.ToString();

					lock (_fileHashesTable)
					{
						if (!_fileHashesTable.TryGetValue(sha, out var index))
						{
							index = _fileHashesTable.Count;
							_fileHashesTable[sha] = index;
						}

						return (sha, index);
					}
				}
			}
		}
	}
}
