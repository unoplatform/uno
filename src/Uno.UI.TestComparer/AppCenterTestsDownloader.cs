using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Refit;

namespace Uno.UI.TestComparer
{
	public class AppCenterTestsDownloader
	{
		private string _secret;

		public AppCenterTestsDownloader(string secret)
		{
			_secret = secret;
		}

		public async Task Download(string testCloudApiKey, string basePath, int runLimit)
		{
			System.Net.ServicePointManager.DefaultConnectionLimit = 40;

			Helpers.WriteLineWithTime($"Downloading {runLimit} test results from testcloud to [{basePath}]");

			var appCenterApi = RestService.For<IAppCenterApi>(
				hostUrl: "https://api.appcenter.ms",
				settings: new RefitSettings
				{
					HttpMessageHandlerFactory = () => new AuthenticatedHttpClientHandler(_secret)
				}
			);

			Helpers.WriteLineWithTime($"Getting apps...");
			var apps = await appCenterApi.GetApps("nventive");
			var unoApps = apps.Where(a => a.DisplayName == "Uno.UI Samples");

			foreach (var app in unoApps)
			{
				Helpers.WriteLineWithTime($"Getting runs for {app.DisplayName}...");
				var runs = await appCenterApi.GetTestRuns(app.Owner.Name, app.Name);

				var validRuns = runs
					.OrderByDescending(r => r.Date)
					.Where(r => !r.State.Equals("running", StringComparison.OrdinalIgnoreCase))
					.Take(runLimit)
					.ToArray()
					;

				foreach (var run in validRuns.Select((v, i) => new { Index = i, Value = v }))
				{
					Helpers.WriteLineWithTime($"Getting run {run.Index + 1} of {validRuns.Length} for {run.Value.Platform} at {run.Value.Date}...");

					var runName = $"{run.Value.Date:yyyyMMdd-hhmmss}-{run.Value.Id}";
					var fullPath = Path.Combine(basePath, run.Value.Platform, runName);

					if (!Directory.Exists(fullPath))
					{
						Directory.CreateDirectory(fullPath);

						await DownloadRun(appCenterApi, app.Owner.Name, app.Name, run.Value.Id, fullPath);
					}
				}
			}
		}

		private async Task DownloadRun(IAppCenterApi appCenterApi, string ownerName, string appName, string runId, string outputPath)
		{
			try
			{
				var runResults2 = await appCenterApi.GetTestReport(ownerName, appName, runId);

				if (runResults2.Features != null)
				{
					var steps = from feature in runResults2.Features
								from test in feature.Tests
								from run in test.Runs
								from step in run.Steps
								select new
								{
									Step = step,
									Run = run,
									Test = test
								};

					steps
						.AsParallel()
						.ForAll(step =>
						{
							var r = StepReport(step.Step).Result;
							var shot = r.DeviceScreenShots
								?.Where(d => !d.Status.Equals("failed", StringComparison.OrdinalIgnoreCase))
								.Select(d => d.Screenshot.Urls.Original).FirstOrDefault();

							var name = step.Step.StepName.StartsWith("-")
								? step.Test.TestName + " " + step.Step.StepName
								: step.Step.StepName;

							if (shot != null)
							{

								int tries = 3;
								while (tries-- > 0)
								{
									try
									{
										string fileName = outputPath + "\\" + name.Replace(" ", "_") + ".png";
										if (!File.Exists(fileName))
										{
											Helpers.WriteLineWithTime($"Downloading ({tries} try) for {name}");
											new WebClient().DownloadFile(shot, fileName);
										}
										else
										{
											Helpers.WriteLineWithTime($"Skipping existing {name}");
										}
										return;
									}
									catch (Exception e)
									{
										Helpers.WriteLineWithTime($"Retrying in 1s... {e.Message}");
										Thread.Sleep(1000);
									}
								}
							}
							else
							{
								Helpers.WriteLineWithTime($"Skipping missing screenshot for {name}");
							}
						});
				}
			}
			catch (Exception e)
			{
				Helpers.WriteLineWithTime($"Run download failed ({e.Message})");
			}
		}

		public async Task<StepReport> StepReport(TestResultTypeStep step)
		{
			var output = await Retriable(() => new WebClient().DownloadStringTaskAsync(step.Step_report_url));

			var result = (StepReport)JsonConvert.DeserializeObject(output, typeof(StepReport));

			return result;
		}

		private async Task<string> Retriable(Func<Task<string>> task)
		{

			int tries = 3;
			do
			{
				try
				{
					return await task();
				}
				catch (WebException e)
				{
					Helpers.WriteLineWithTime($"Retrying in 1s (2) {e.Message}");
					await Task.Delay(1000);
				}
			} while (tries-- > 0);

			throw new("Retry failed");
		}
	}

	public interface IAppCenterApi
	{
		[Get("/v0.1/apps/{owner_name}/{app_name}/test_runs")]
		Task<IList<TestRun>> GetTestRuns([AliasAs("owner_name")] string ownerName, [AliasAs("app_name")] string appName);

		[Get("/v0.1/orgs/{org_name}/apps")]
		Task<IList<OrganizationApp>> GetApps([AliasAs("org_name")] string organizationName);

		[Get("/v0.1/apps/{owner_name}/{app_name}/test_runs/{test_run_id}/report")]
		Task<TestResultType> GetTestReport(
			[AliasAs("owner_name")] string ownerName,
			[AliasAs("app_name")] string appName,
			[AliasAs("test_run_id")] string testRunId
		);
	}

	public class OrganizationApp
	{
		public Guid Id { get; set; }

		public string AppSecret { get; set; }

		public string Name { get; set; }

		[JsonProperty(PropertyName = "display_name")]
		public string DisplayName { get; set; }

		public Organization Owner { get; set; }
	}

	public class Organization
	{
		public string Id { get; set; }

		public string DisplayName { get; set; }

		public string Name { get; set; }
	}

	public class TestRun
	{
		[JsonProperty(PropertyName = "id")]
		public string Id { get; set; }

		[JsonProperty(PropertyName = "date")]
		public DateTimeOffset Date { get; set; }

		[JsonProperty(PropertyName = "platform")]
		public string Platform { get; set; }

		[JsonProperty(PropertyName = "state")]
		public string State { get; set; }

		[JsonProperty(PropertyName = "status")]
		public string Status { get; set; }

		[JsonProperty(PropertyName = "resultStatus")]
		public string ResultStatus { get; set; }

		[JsonProperty(PropertyName = "appVersion")]
		public string ApPVersion { get; set; }
	}

	public class TestResultType
	{
		public Guid Id { get; set; }
		public DateTimeOffset Date { get; set; }
		public string TestType { get; set; }
		public TestResultTypeFeature[] Features { get; set; }
	}

	public class TestResultTypeFeature
	{
		public string Name { get; set; }
		public TestResultTypeTest[] Tests { get; set; }
	}

	public class TestResultTypeTest
	{
		public string TestName { get; set; }

		public TestResultTypeRun[] Runs { get; set; }
	}

	public class TestResultTypeRun
	{
		public TestResultTypeStep[] Steps { get; set; }
		public Uri Report_Url { get; set; }
		public int Failed { get; set; }
	}

	public class TestResultTypeStep
	{
		public string Id { get; set; }
		public string StepName { get; set; }
		public Uri Step_report_url { get; set; }
	}

	public class StepReport
	{
		public string[] FinishedSnapshots { get; set; }

		public StepReportDeviceScreenShot[] DeviceScreenShots { get; set; }
	}

	public class StepReportDeviceScreenShot
	{
		public string Id { get; set; }

		// public string[] StackTrace { get; set; }

		public string Title { get; set; }

		public string Status { get; set; }

		public StepReportDeviceScreenShotDescription Screenshot { get; set; }
	}

	public class StepReportDeviceScreenShotDescription
	{
		public int Rotation { get; set; }
		public bool Landscape { get; set; }

		public StepReportDeviceScreenShotUrls Urls { get; set; }
	}

	public class StepReportDeviceScreenShotUrls
	{
		public Uri Original { get; set; }
	}

	class AuthenticatedHttpClientHandler : HttpClientHandler
	{
		private readonly string _secret;

		public AuthenticatedHttpClientHandler(string secret)
		{
			_secret = secret ?? throw new ArgumentNullException("getToken");
		}

		protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			// See if the request has an authorize header
			if (!request.Headers.Contains("X-API-Token"))
			{
				request.Headers.Add("X-API-Token", _secret);
			}

			return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
		}
	}
}
