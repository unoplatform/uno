#nullable enable
#pragma warning disable CS8305

#if HAS_UNO

using System;
using System.Linq;
using System.Threading.Tasks;
using Private.Infrastructure;
using Windows.UI.Shell.Tasks;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Shell_Tasks;

[TestClass]
public class Given_AppTaskInfo
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23752")]
	public async Task When_Task_Is_Managed_In_A_Running_App()
	{
		for (var attempt = 0; attempt < 50 && !AppTaskInfo.IsSupported(); attempt++)
		{
			await Task.Delay(100);
		}

		if (!AppTaskInfo.IsSupported())
		{
			Assert.AreEqual(0, AppTaskInfo.FindAll().Length);
			return;
		}

		RemoveAllTasks();

		AppTaskInfo? task = null;
		try
		{
			task = AppTaskInfo.Create(
				"Runtime task",
				"Running in SamplesApp",
				new Uri("sample-app://tasks/runtime"),
				new Uri("ms-appx:///Assets/StoreLogo.png"),
				AppTaskContent.CreateSequenceOfSteps(Array.Empty<string>(), "Starting"));

			var created = AppTaskInfo.FindAll().Single();
			Assert.AreEqual(task.Id, created.Id);
			Assert.AreEqual(AppTaskState.Running, created.State);
			Assert.AreEqual("Starting", created.GetExecutingStep());

			await Task.Run(() =>
			{
				task.UpdateTitles("Runtime task updated", "Background-thread update");
				task.UpdateDeepLink(new Uri("sample-app://tasks/runtime/updated"));
				task.Update(
					AppTaskState.Completed,
					AppTaskContent.CreateTextSummaryResult("Completed in the runtime-test app"));
			});

			var updated = AppTaskInfo.FindAll().Single();
			Assert.AreEqual("Runtime task updated", updated.Title);
			Assert.AreEqual("Background-thread update", updated.Subtitle);
			Assert.AreEqual(new Uri("sample-app://tasks/runtime/updated"), updated.DeepLink);
			Assert.AreEqual(AppTaskState.Completed, updated.State);
			Assert.IsNotNull(updated.EndTime);

			task.Remove();
			Assert.AreEqual(0, AppTaskInfo.FindAll().Length);
		}
		finally
		{
			task?.Remove();
			RemoveAllTasks();
		}
	}

	private static void RemoveAllTasks()
	{
		foreach (var task in AppTaskInfo.FindAll())
		{
			task.Remove();
		}
	}
}

#endif
