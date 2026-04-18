using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.Shell.Tasks;

namespace UITests.Windows_UI_Shell;

[Sample("Windows.UI.Shell.Tasks", Name = "AppTaskInfo", IsManualTest = true, Description = "Creates, updates, and removes app tasks. Inspect the taskbar on Win32 or the Dock badge on macOS while the sample is running.")]
public sealed partial class AppTaskInfoTests : Page
{
	private static readonly string[] _primarySteps =
	[
		"Index source files",
		"Generate release notes",
		"Package output",
	];

	private readonly List<AppTaskInfo> _trackedTasks = new();
	private readonly List<string> _logEntries = new();
	private AppTaskInfo _primaryTask;
	private int _primaryStepIndex;
	private int _secondaryTaskCounter;

	public AppTaskInfoTests()
	{
		this.InitializeComponent();
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
	}

	private static Uri DemoDeepLink => new Uri("sample-app://shelltasks/open");
	private static Uri DemoIconUri => new Uri("ms-appx:///Assets/bluecrystal.ico");
	private static Uri DemoPreviewUri => new Uri("ms-appx:///Assets/ingredient1.png");
	private static Uri DemoGeneratedAssetUri => new Uri("ms-appx:///Assets/Uno200x200.png");

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		Log("Sample loaded.");
		RefreshVisualState();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		ClearTrackedTasks(logAction: false);
	}

	private void CreatePrimaryTask_Click(object sender, RoutedEventArgs e)
	{
		if (!EnsureSupported())
		{
			return;
		}

		if (_primaryTask is not null)
		{
			RemoveTrackedTask(_primaryTask, logAction: false);
		}

		_primaryStepIndex = 0;
		_primaryTask = AppTaskInfo.Create(
			title: "Publish release notes",
			subtitle: "Preparing changelog",
			deepLink: DemoDeepLink,
			iconUri: DemoIconUri,
			content: CreateSequenceContent(_primaryStepIndex));

		TrackTask(_primaryTask);
		Log($"Created primary task '{_primaryTask.Title}'.");
		RefreshVisualState();
	}

	private void AdvancePrimaryTask_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		if (_primaryStepIndex >= _primarySteps.Length - 1)
		{
			CompletePrimaryTask(task, "Completed the primary task using text summary content.");
			return;
		}

		_primaryStepIndex++;
		task.Update(AppTaskState.Running, CreateSequenceContent(_primaryStepIndex));
		Log($"Advanced the primary task to '{_primarySteps[_primaryStepIndex]}'.");
		RefreshVisualState();
	}

	private void AddSecondaryTask_Click(object sender, RoutedEventArgs e)
	{
		if (!EnsureSupported())
		{
			return;
		}

		_secondaryTaskCounter++;

		var task = AppTaskInfo.Create(
			title: $"Background sync {_secondaryTaskCounter}",
			subtitle: "Uploading assets",
			deepLink: CreateDeepLink($"secondary-{_secondaryTaskCounter}"),
			iconUri: DemoIconUri,
			content: CreatePreviewContent($"Syncing batch {_secondaryTaskCounter}"));

		TrackTask(task);
		Log($"Created extra running task '{task.Title}'.");
		RefreshVisualState();
	}

	private void RefreshTasks_Click(object sender, RoutedEventArgs e)
	{
		RefreshVisualState();
		Log("Refreshed the task list.");
	}

	private void PausePrimaryTask_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		task.UpdateState(AppTaskState.Paused);
		Log("Paused the primary task.");
		RefreshVisualState();
	}

	private void NeedsAttentionPrimaryTask_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		task.UpdateState(AppTaskState.NeedsAttention);
		Log("Marked the primary task as needing attention.");
		RefreshVisualState();
	}

	private void ResumePrimaryTask_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		task.Update(AppTaskState.Running, CreateSequenceContent(_primaryStepIndex));
		Log("Resumed the primary task with sequence content.");
		RefreshVisualState();
	}

	private void FailPrimaryTask_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		task.Update(AppTaskState.Error, AppTaskContent.CreateTextSummaryResult("Upload failed. Check your network connection and try again."));
		Log("Marked the primary task as failed.");
		RefreshVisualState();
	}

	private void RemovePrimaryTask_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		RemoveTrackedTask(task);
		Log("Removed the primary task.");
		RefreshVisualState();
	}

	private void ShowPreviewContent_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		task.Update(AppTaskState.Running, CreatePreviewContent("Rendering thumbnail preview"));
		Log("Updated the primary task with preview thumbnail content.");
		RefreshVisualState();
	}

	private void ShowGeneratedAssets_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		task.Update(AppTaskState.Completed, CreateGeneratedAssetsContent());
		Log("Updated the primary task with generated assets content.");
		RefreshVisualState();
	}

	private void CompletePrimaryTask_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		CompletePrimaryTask(task, "Completed the primary task using text summary content.");
	}

	private void ClearAllTasks_Click(object sender, RoutedEventArgs e)
	{
		ClearTrackedTasks();
	}

	private void CompletePrimaryTask(AppTaskInfo task, string logMessage)
	{
		task.Update(AppTaskState.Completed, AppTaskContent.CreateTextSummaryResult("Release notes exported successfully."));
		Log(logMessage);
		RefreshVisualState();
	}

	private void TrackTask(AppTaskInfo task)
	{
		_trackedTasks.Add(task);
	}

	private void RemoveTrackedTask(AppTaskInfo task, bool logAction = false)
	{
		task.Remove();
		_trackedTasks.Remove(task);

		if (ReferenceEquals(task, _primaryTask))
		{
			_primaryTask = null;
			_primaryStepIndex = 0;
		}

		if (logAction)
		{
			Log($"Removed '{task.Title}'.");
		}
	}

	private void ClearTrackedTasks(bool logAction = true)
	{
		foreach (var task in _trackedTasks.ToArray())
		{
			task.Remove();
		}

		_trackedTasks.Clear();
		_primaryTask = null;
		_primaryStepIndex = 0;
		_secondaryTaskCounter = 0;

		if (logAction)
		{
			Log("Cleared all created app tasks.");
		}

		RefreshVisualState();
	}

	private bool EnsureSupported()
	{
		if (AppTaskInfo.IsSupported())
		{
			return true;
		}

		Log("AppTaskInfo is not supported on this platform.");
		RefreshVisualState();
		return false;
	}

	private bool TryGetPrimaryTask(out AppTaskInfo task)
	{
		task = _primaryTask;
		if (task is not null)
		{
			return true;
		}

		Log("Create the primary task first.");
		RefreshVisualState();
		return false;
	}

	private void RefreshVisualState()
	{
		var isSupported = AppTaskInfo.IsSupported();
		var activeTasks = isSupported ? AppTaskInfo.FindAll() : Array.Empty<AppTaskInfo>();

		SupportTextBlock.Text = isSupported
			? "AppTaskInfo is supported here. On Win32 watch the taskbar button; on macOS watch the Dock badge."
			: "AppTaskInfo is not currently supported on this platform, so the action buttons are disabled.";
		ControlsPanel.IsHitTestVisible = isSupported;
		ControlsPanel.Opacity = isSupported ? 1d : 0.6d;
		PrimaryTaskTextBlock.Text = GetPrimaryTaskDescription();
		TasksSummaryTextBlock.Text = $"Active tasks reported by AppTaskInfo.FindAll(): {activeTasks.Length}";
		TasksListView.ItemsSource = activeTasks.Length > 0
			? activeTasks.Select(FormatTask).ToArray()
			: new[] { "No active app tasks." };
		LogListView.ItemsSource = _logEntries.ToArray();
	}

	private string GetPrimaryTaskDescription()
	{
		if (_primaryTask is null)
		{
			return "Primary task: not created.";
		}

		var completedSteps = _primaryTask.GetCompletedSteps();
		var completedDescription = completedSteps.Length > 0
			? $"Completed: {string.Join(" → ", completedSteps)}"
			: "Completed: none";
		var executingStep = _primaryTask.GetExecutingStep();
		var executingDescription = string.IsNullOrWhiteSpace(executingStep)
			? "Current step: none"
			: $"Current step: {executingStep}";

		return $"Primary task: {_primaryTask.Title} — {_primaryTask.State}. {completedDescription}. {executingDescription}.";
	}

	private void Log(string message)
	{
		_logEntries.Insert(0, $"{DateTime.Now:HH:mm:ss} — {message}");
		LogListView.ItemsSource = _logEntries.ToArray();
	}

	private static string FormatTask(AppTaskInfo task)
	{
		var completedSteps = task.GetCompletedSteps();
		var completedDescription = completedSteps.Length > 0
			? $"Completed: {string.Join(" → ", completedSteps)}"
			: "Completed: none";
		var executingStep = string.IsNullOrWhiteSpace(task.GetExecutingStep())
			? "Current step: none"
			: $"Current step: {task.GetExecutingStep()}";
		var endedAt = task.EndTime is { } endTime
			? $" | Ended: {endTime:HH:mm:ss}"
			: string.Empty;

		return $"{task.Title} — {task.State} | {task.Subtitle} | {completedDescription} | {executingStep}{endedAt}";
	}

	private static AppTaskContent CreateSequenceContent(int currentStepIndex)
	{
		var completedSteps = _primarySteps.Take(currentStepIndex).ToArray();
		var content = AppTaskContent.CreateSequenceOfSteps(completedSteps, _primarySteps[Math.Min(currentStepIndex, _primarySteps.Length - 1)]);
		content.SetQuestion("Leave this task running in the background?");
		content.AddButton("Open details", CreateDeepLink("details"));
		content.AddButton("Pause", CreateDeepLink("pause"));
		content.SetTextInput("Add a note", "sample-app://shelltasks/note?text={0}");
		return content;
	}

	private static AppTaskContent CreatePreviewContent(string executingStep)
	{
		var content = AppTaskContent.CreatePreviewThumbnail(DemoPreviewUri, executingStep);
		content.AddButton("Open preview", CreateDeepLink("preview"));
		return content;
	}

	private static AppTaskContent CreateGeneratedAssetsContent()
	{
		var content = AppTaskContent.CreateGeneratedAssetsResult(
		[
			new AppTaskResultAsset("ReleaseNotes.md", "Generated markdown summary", DemoIconUri, DemoGeneratedAssetUri),
			new AppTaskResultAsset("Preview.png", "Preview image", DemoIconUri, DemoPreviewUri),
		]);
		content.AddButton("Open output", CreateDeepLink("output"));
		return content;
	}

	private static Uri CreateDeepLink(string action)
	{
		return new Uri($"sample-app://shelltasks/{action}");
	}
}
