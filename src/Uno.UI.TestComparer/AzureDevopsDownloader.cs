using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
using Refit;

namespace Uno.UI.TestComparer
{
	class AzureDevopsDownloader
	{
		private readonly string _pat;
		private readonly string _collectionUri;

		public AzureDevopsDownloader(string pat, string collectionUri)
		{
			_pat = pat;
			_collectionUri = collectionUri;
		}

		public async Task<string[]> DownloadArtifacts(string basePath,
									  string project,
									  string definitionName,
									  string artifactName,
									  string sourceBranch,
									  string targetBranchName,
									  int buildId,
									  int runLimit)
		{
			Directory.CreateDirectory(basePath);
			var gettingBuildTime = TimeSpan.Zero;
			var downloadZipTime = TimeSpan.Zero;
			var extractZipTime = TimeSpan.Zero;
			var timer = new System.Diagnostics.Stopwatch();
			var connection = new VssConnection(new Uri(_collectionUri), new VssBasicCredential(string.Empty, _pat));

			var client = await connection.GetClientAsync<BuildHttpClient>();

			if (!targetBranchName.StartsWith("refs/heads/"))
			{
				targetBranchName = "refs/heads/" + targetBranchName;
			}

			timer.Start();
			Console.Write($"Getting definitions ({basePath}, {project}, {definitionName}, {artifactName}, {sourceBranch}, {targetBranchName}, {buildId}, {runLimit}) ");
			var definitions = await client.GetDefinitionsAsync(project, name: definitionName);
			timer.Stop();
			Console.WriteLine(timer.Elapsed);

			Console.Write("Getting builds ");
			timer.Restart();
			var builds = await client.GetBuildsAsync(
				project,
				definitions: new[] { definitions.First().Id },
				branchName: targetBranchName,
				top: runLimit,
				queryOrder: BuildQueryOrder.FinishTimeDescending,
				statusFilter: BuildStatus.Completed,
				resultFilter: BuildResult.Succeeded);

			var currentBuild = await client.GetBuildAsync(project, buildId);

			var suceededBuilds = builds
				.Distinct(new BuildComparer())
				.OrderBy(b => b.FinishTime)
				.Concat(new[] { currentBuild })
				.ToList(); // Add ToList() for mesure comparing.
			timer.Stop();
			Console.WriteLine(timer.Elapsed);
			gettingBuildTime += timer.Elapsed;

			string BuildArtifactPath(Build build)
				=> Path.Combine(basePath, $@"artifacts\{build.LastChangedDate:yyyyMMdd-hhmmss}-{build.Id}");

			foreach (var build in suceededBuilds)
			{
				var fullPath = BuildArtifactPath(build);

				if (!Directory.Exists(fullPath))
				{
					var tempFile = Path.GetTempFileName();

					var artifacts = await client.GetArtifactsAsync(project, build.Id);

					if (artifacts.Any(a => a.Name == artifactName))
					{						
						Console.Write($"Getting artifact for build {build.Id} ... ");
						timer.Restart();
						using (var stream = await client.GetArtifactContentZipAsync(project, build.Id, artifactName))
						{
							using (var f = File.OpenWrite(tempFile))
							{
								await stream.CopyToAsync(f);
							}
						}
						timer.Stop();
						Console.WriteLine(timer.Elapsed);
						downloadZipTime += timer.Elapsed;

						Console.Write($"Extracting artifact for build {build.Id} ... ");
						timer.Restart();
						fullPath = fullPath.Replace("\\\\", "\\");

						using (var archive = ZipFile.OpenRead(tempFile))
						{
							foreach (var entry in archive.Entries)
							{
								var outPath = Path.Combine(fullPath, entry.FullName.Replace("/", "\\"));

								if (outPath.EndsWith("\\"))
								{
									Directory.CreateDirectory(@"\\?\" + outPath);
								}
								else
								{
									using (var stream = entry.Open())
									{
										using (var outStream = File.OpenWrite(@"\\?\" + outPath))
										{
											await stream.CopyToAsync(outStream);
										}
									}
								}
							}
						}
						timer.Stop();
						Console.WriteLine(timer.Elapsed);
						extractZipTime += timer.Elapsed;
					}
					else
					{
						Console.WriteLine($"Skipping download artifact for build {build.Id} (The artifact {artifactName} cannot be found)");
					}
				}
				else
				{
					Console.WriteLine($"Skipping already downloaded build {build.Id} artifacts");
				}
			}

			Console.WriteLine($"Total Getting Builds\t{gettingBuildTime}");
			Console.WriteLine($"Total Downlaod Zip\t{downloadZipTime}");
			Console.WriteLine($"Total Extract Zip\t{extractZipTime}");

			return suceededBuilds.Select(BuildArtifactPath).ToArray();
		}

		private class BuildComparer : IEqualityComparer<Build>
		{
			public bool Equals(Build x, Build y) => x.Id == y.Id;
			public int GetHashCode(Build obj) => obj.Id.GetHashCode();
		}

	}
}
