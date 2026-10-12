#nullable enable
#pragma warning disable CS8305

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Samples.Controls;
using Windows.UI.Shell.Tasks;

namespace UITests.Shared.Windows_UI_Shell;

[Sample("Windows.UI.Shell.Tasks", Name = "AppTaskInfo", IsManualTest = true, Description = "Creates, restores, updates, and removes app tasks while exposing each platform's shell approximation.")]
public sealed partial class AppTaskInfoTests : Page
{
	private static readonly string[] _primarySteps =
	[
		"Index source files",
		"Generate release notes",
		"Package output",
	];

	private readonly List<AppTaskInfo> _trackedTasks = new();
	private readonly Dictionary<string, string> _contentDescriptions = new(StringComparer.Ordinal);
	private readonly List<string> _logEntries = new();
	private AppTaskInfo? _primaryTask;
	private int _primaryStepIndex;
	private int _secondaryTaskCounter;

	public AppTaskInfoTests()
	{
		this.InitializeComponent();
		Loaded += OnLoaded;
	}

	private static Uri DemoDeepLink => new Uri("sample-app://shelltasks/open");
	private static Uri DemoIconUri => new Uri("ms-appx:///Assets/bluecrystal.ico");
	private static Uri DemoPreviewUri => new Uri("ms-appx:///Assets/ingredient1.png");
	private static Uri DemoGeneratedAssetUri => new Uri("ms-appx:///Assets/Uno200x200.png");

	private async void OnLoaded(object sender, RoutedEventArgs e)
	{
		try
		{
#if __ANDROID__
			RequestAndroidNotificationPermission();
#endif
			for (var attempt = 0; attempt < 50 && !AppTaskInfo.IsSupported(); attempt++)
			{
				await Task.Delay(100);
			}

			if (!IsLoaded)
			{
				return;
			}

			ReloadPersistedTasks();
			Log($"Sample loaded with {_trackedTasks.Count} persisted task(s).");
			RefreshVisualState();
		}
		catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException)
		{
			Log($"Unable to initialize AppTaskInfo: {error.Message}");
			SupportTextBlock.Text = "AppTaskInfo initialization failed. See the action log for details.";
			ControlsPanel.IsHitTestVisible = false;
			ControlsPanel.Opacity = 0.6d;
		}
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
		var content = CreateSequenceContent(_primaryStepIndex);
		_primaryTask = AppTaskInfo.Create(
			title: "Publish release notes",
			subtitle: "Preparing changelog",
			deepLink: DemoDeepLink,
			iconUri: DemoIconUri,
			content: content.Content);

		TrackTask(_primaryTask, content.Description);
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
		UpdateTask(task, AppTaskState.Running, CreateSequenceContent(_primaryStepIndex));
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

		var content = CreatePreviewContent($"Syncing batch {_secondaryTaskCounter}");
		var task = AppTaskInfo.Create(
			title: $"Background sync {_secondaryTaskCounter}",
			subtitle: "Uploading assets",
			deepLink: CreateDeepLink($"secondary-{_secondaryTaskCounter}"),
			iconUri: DemoIconUri,
			content: content.Content);

		TrackTask(task, content.Description);
		Log($"Created extra running task '{task.Title}'.");
		RefreshVisualState();
	}

	private void RefreshTasks_Click(object sender, RoutedEventArgs e)
	{
		ReloadPersistedTasks();
		RefreshVisualState();
		Log("Reloaded the persisted task list.");
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

		// NeedsAttention requires content that carries a question, so the state is updated together
		// with sequence content that sets one.
		UpdateTask(task, AppTaskState.NeedsAttention, CreateSequenceContent(_primaryStepIndex));
		Log("Marked the primary task as needing attention.");
		RefreshVisualState();
	}

	private void ResumePrimaryTask_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		UpdateTask(task, AppTaskState.Running, CreateSequenceContent(_primaryStepIndex));
		Log("Resumed the primary task with sequence content.");
		RefreshVisualState();
	}

	private void FailPrimaryTask_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		UpdateTask(task, AppTaskState.Error, CreateTextSummary("Upload failed. Check your network connection and try again."));
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

		UpdateTask(task, AppTaskState.Running, CreatePreviewContent("Rendering thumbnail preview"));
		Log("Updated the primary task with preview thumbnail content.");
		RefreshVisualState();
	}

	private void ShowGeneratedAssets_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		UpdateTask(task, AppTaskState.Completed, CreateGeneratedAssetsContent());
		Log("Updated the primary task with generated assets content.");
		RefreshVisualState();
	}

	private void UpdateDeepLinkPrimaryTask_Click(object sender, RoutedEventArgs e)
	{
		if (!TryGetPrimaryTask(out var task))
		{
			return;
		}

		var deepLink = CreateDeepLink($"details/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}");
		task.UpdateDeepLink(deepLink);
		Log($"Updated the primary task deep link to '{deepLink}'.");
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
		UpdateTask(task, AppTaskState.Completed, CreateTextSummary("Release notes exported successfully."));
		Log(logMessage);
		RefreshVisualState();
	}

	private void UpdateTask(AppTaskInfo task, AppTaskState state, SampleContent content)
	{
		task.Update(state, content.Content);
		_contentDescriptions[task.Id] = content.Description;
	}

	private void TrackTask(AppTaskInfo task, string contentDescription)
	{
		if (_trackedTasks.All(existing => existing.Id != task.Id))
		{
			_trackedTasks.Add(task);
		}

		_contentDescriptions[task.Id] = contentDescription;
	}

	private void RemoveTrackedTask(AppTaskInfo task, bool logAction = false)
	{
		task.Remove();
		_trackedTasks.Remove(task);
		_contentDescriptions.Remove(task.Id);

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
		var tasks = AppTaskInfo.IsSupported()
			? AppTaskInfo.FindAll()
			: _trackedTasks.ToArray();
		foreach (var task in tasks)
		{
			task.Remove();
		}

		_trackedTasks.Clear();
		_contentDescriptions.Clear();
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
		if (_primaryTask is { } primaryTask)
		{
			task = primaryTask;
			return true;
		}

		task = null!;
		Log("Create the primary task first.");
		RefreshVisualState();
		return false;
	}

	private void RefreshVisualState()
	{
		var isSupported = AppTaskInfo.IsSupported();
		var activeTasks = isSupported ? AppTaskInfo.FindAll() : Array.Empty<AppTaskInfo>();

		SupportTextBlock.Text = isSupported
			? "AppTaskInfo is supported. The in-app list shows the persisted public task properties; the external shell representation varies by platform."
			: "AppTaskInfo is not currently supported. Android 13 or later requires notification permission; Linux requires a D-Bus notification service.";
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

		return $"Primary task: {_primaryTask.Title} — {_primaryTask.State}. ID: {_primaryTask.Id}. Hidden: {_primaryTask.HiddenByUser}. {DescribeContent(_primaryTask)}";
	}

	// The public AppTaskInfo API only exposes step data after a restore, so content created in this session
	// is described from what the sample applied, and restored content is described from its steps.
	private string DescribeContent(AppTaskInfo task)
	{
		if (_contentDescriptions.TryGetValue(task.Id, out var description))
		{
			return description;
		}

		try
		{
			return $"{DescribeSteps(task.GetCompletedSteps(), task.GetExecutingStep())} Restored preview, summary, and asset details are not exposed by AppTaskInfo.";
		}
		catch (ArgumentException)
		{
			// Tasks created without content reject the step accessors.
			return "Content: none.";
		}
	}

	private static int GetCompletedStepCount(AppTaskInfo task)
	{
		try
		{
			return task.GetCompletedSteps().Length;
		}
		catch (ArgumentException)
		{
			return 0;
		}
	}

	private static string DescribeSteps(string[] completedSteps, string executingStep)
	{
		var completedDescription = completedSteps.Length > 0
			? $"Completed: {string.Join(" → ", completedSteps)}"
			: "Completed: none";
		var executingDescription = string.IsNullOrWhiteSpace(executingStep)
			? "Current step: none"
			: $"Current step: {executingStep}";

		return $"{completedDescription}. {executingDescription}.";
	}

	private void Log(string message)
	{
		_logEntries.Insert(0, $"{DateTime.Now:HH:mm:ss} — {message}");
		LogListView.ItemsSource = _logEntries.ToArray();
	}

	private string FormatTask(AppTaskInfo task)
	{
		var endedAt = task.EndTime is { } endTime
			? $" | Ended: {endTime:HH:mm:ss}"
			: string.Empty;

		return $"{task.Title} — {task.State} | ID: {task.Id} | Hidden: {task.HiddenByUser} | {task.Subtitle} | {DescribeContent(task)}{endedAt}";
	}

	private static SampleContent CreateSequenceContent(int currentStepIndex)
	{
		var completedSteps = _primarySteps.Take(currentStepIndex).ToArray();
		var executingStep = _primarySteps[Math.Min(currentStepIndex, _primarySteps.Length - 1)];
		var content = AppTaskContent.CreateSequenceOfSteps(completedSteps, executingStep);
		content.SetQuestion("Leave this task running in the background?");
		content.AddButton("Open details", CreateDeepLink("details"));
		content.AddButton("Pause", CreateDeepLink("pause"));
		content.SetTextInput("Add a note", "sample-app://shelltasks/note?text={userTextInput}");
		return new(content, DescribeSteps(completedSteps, executingStep));
	}

	private static SampleContent CreatePreviewContent(string executingStep)
	{
		var content = AppTaskContent.CreatePreviewThumbnail(DemoPreviewUri, executingStep);
		content.AddButton("Open preview", CreateDeepLink("preview"));
		return new(content, $"Preview thumbnail ({DemoPreviewUri}). Current step: {executingStep}.");
	}

	private static SampleContent CreateTextSummary(string text) =>
		new(AppTaskContent.CreateTextSummaryResult(text), $"Text summary: {text}");

	private static SampleContent CreateGeneratedAssetsContent()
	{
		var content = AppTaskContent.CreateGeneratedAssetsResult(
		[
			new AppTaskResultAsset("ReleaseNotes.md", "Generated markdown summary", DemoIconUri, DemoGeneratedAssetUri),
			new AppTaskResultAsset("Preview.png", "Preview image", DemoIconUri, DemoPreviewUri),
		]);
		content.AddButton("Open output", CreateDeepLink("output"));
		return new(content, "Generated assets: ReleaseNotes.md (Generated markdown summary), Preview.png (Preview image).");
	}

	private static Uri CreateDeepLink(string action)
	{
		return new Uri($"sample-app://shelltasks/{action}");
	}

	private void ReloadPersistedTasks()
	{
		_trackedTasks.Clear();
		if (!AppTaskInfo.IsSupported())
		{
			_primaryTask = null;
			return;
		}

		_trackedTasks.AddRange(AppTaskInfo.FindAll());
		_primaryTask = _trackedTasks.FirstOrDefault(task => task.Title.StartsWith("Publish release notes", StringComparison.Ordinal));
		_primaryStepIndex = _primaryTask is null ? 0 : Math.Min(GetCompletedStepCount(_primaryTask), _primarySteps.Length - 1);
	}

#if __ANDROID__
	private void RequestAndroidNotificationPermission()
	{
		if (OperatingSystem.IsAndroidVersionAtLeast(33)
			&& Uno.UI.ContextHelper.Current is Android.App.Activity activity
			&& activity.CheckSelfPermission(Android.Manifest.Permission.PostNotifications) != Android.Content.PM.Permission.Granted)
		{
			activity.RequestPermissions([Android.Manifest.Permission.PostNotifications], requestCode: 23752);
			Log("Requested Android notification permission. After granting it, select 'Reload persisted tasks'.");
		}
	}
#endif

	private readonly record struct SampleContent(AppTaskContent Content, string Description);
}
