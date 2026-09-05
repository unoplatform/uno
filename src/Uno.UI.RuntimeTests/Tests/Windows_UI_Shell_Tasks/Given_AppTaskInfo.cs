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

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23752")]
	public async Task When_State_Becomes_Undefined_Then_Task_Is_Evicted()
	{
		if (!await WaitForSupport())
		{
			Assert.AreEqual(0, AppTaskInfo.FindAll().Length);
			return;
		}

		RemoveAllTasks();

		AppTaskInfo? keeper = null;
		AppTaskInfo? evicted = null;
		try
		{
			keeper = CreateTask("Kept task");
			evicted = CreateTask("Evicted task");
			Assert.AreEqual(2, AppTaskInfo.FindAll().Length);

			evicted.UpdateState((AppTaskState)5);

			Assert.AreEqual((AppTaskState)5, evicted.State);
			Assert.AreEqual(1, AppTaskInfo.FindAll().Length);
			Assert.AreEqual(keeper.Id, AppTaskInfo.FindAll().Single().Id);

			// The eviction survives a round-trip through the real task store.
			evicted.UpdateState(AppTaskState.Running);
			Assert.AreEqual(1, AppTaskInfo.FindAll().Length);
			Assert.AreEqual(keeper.Id, AppTaskInfo.FindAll().Single().Id);
		}
		finally
		{
			evicted?.Remove();
			keeper?.Remove();
			RemoveAllTasks();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23752")]
	public async Task When_NeedsAttention_Has_No_Question_Then_It_Is_Rejected()
	{
		if (!await WaitForSupport())
		{
			Assert.AreEqual(0, AppTaskInfo.FindAll().Length);
			return;
		}

		RemoveAllTasks();

		AppTaskInfo? task = null;
		try
		{
			task = CreateTask("Attention task");

			Assert.ThrowsExactly<ArgumentException>(() => task.UpdateState(AppTaskState.NeedsAttention));
			Assert.AreEqual(AppTaskState.Running, task.State);
			Assert.AreEqual(AppTaskState.Running, AppTaskInfo.FindAll().Single().State);

			var withQuestion = AppTaskContent.CreateTextSummaryResult("Waiting for input");
			withQuestion.SetQuestion("Continue?");
			task.Update(AppTaskState.NeedsAttention, withQuestion);

			Assert.AreEqual(AppTaskState.NeedsAttention, task.State);
			Assert.AreEqual(AppTaskState.NeedsAttention, AppTaskInfo.FindAll().Single().State);
		}
		finally
		{
			task?.Remove();
			RemoveAllTasks();
		}
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/23752")]
	public async Task When_Task_Is_Created_Without_Content_Then_Accessors_Are_Rejected()
	{
		if (!await WaitForSupport())
		{
			Assert.AreEqual(0, AppTaskInfo.FindAll().Length);
			return;
		}

		RemoveAllTasks();

		AppTaskInfo? task = null;
		try
		{
			task = AppTaskInfo.Create(
				"Content-less task",
				"No content",
				new Uri("sample-app://tasks/no-content"),
				new Uri("ms-appx:///Assets/StoreLogo.png"),
				null!);

			Assert.AreEqual(1, AppTaskInfo.FindAll().Length);
			Assert.ThrowsExactly<ArgumentException>(() => task.GetExecutingStep());
			Assert.ThrowsExactly<ArgumentException>(() => AppTaskInfo.FindAll().Single().GetCompletedSteps());

			task.Update(AppTaskState.Running, AppTaskContent.CreateSequenceOfSteps(["One"], "Two"));
			Assert.AreEqual("Two", AppTaskInfo.FindAll().Single().GetExecutingStep());
		}
		finally
		{
			task?.Remove();
			RemoveAllTasks();
		}
	}

	private static async Task<bool> WaitForSupport()
	{
		for (var attempt = 0; attempt < 50 && !AppTaskInfo.IsSupported(); attempt++)
		{
			await Task.Delay(100);
		}

		return AppTaskInfo.IsSupported();
	}

	private static AppTaskInfo CreateTask(string title) =>
		AppTaskInfo.Create(
			title,
			"Running in SamplesApp",
			new Uri("sample-app://tasks/runtime"),
			new Uri("ms-appx:///Assets/StoreLogo.png"),
			AppTaskContent.CreateTextSummaryResult(title));

	private static void RemoveAllTasks()
	{
		foreach (var task in AppTaskInfo.FindAll())
		{
			task.Remove();
		}
	}
}

#endif
