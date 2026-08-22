// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// WinSDK Reference windows.ui.shell.tasks.idl, Windows SDK 10.0.26100.7705, commit 1bfb76d

#nullable enable
#pragma warning disable CS8305

using System;
using Windows.Foundation.Metadata;

namespace Windows.UI.Shell.Tasks;

/// <summary>
/// Represents an app task that can be displayed in the Windows Shell.
/// </summary>
[ContractVersion(typeof(AppTaskContract), 65536U)]
[Experimental]
[MarshalingBehavior(MarshalingType.Agile)]
[Threading(ThreadingModel.Both)]
public sealed class AppTaskInfo
{
	private readonly object _snapshotGate = new();
	private readonly string _id;
	private AppTaskInfoSnapshot _lastKnownSnapshot;

	internal AppTaskInfo(AppTaskInfoSnapshot snapshot)
	{
		_id = snapshot.Id;
		_lastKnownSnapshot = snapshot;
	}

	/// <summary>
	/// Gets the title of this task.
	/// </summary>
	public string Title => GetCurrentSnapshot().Title;

	/// <summary>
	/// Gets the subtitle of this task.
	/// </summary>
	public string Subtitle => GetCurrentSnapshot().Subtitle;

	/// <summary>
	/// Gets a URI that will be launched when the user clicks on the task's Shell representation.
	/// </summary>
	public Uri DeepLink => GetCurrentSnapshot().DeepLink;

	/// <summary>
	/// Gets the path to an icon that represents the task.
	/// </summary>
	public Uri IconUri => GetCurrentSnapshot().IconUri;

	/// <summary>
	/// Gets the current state of this task.
	/// </summary>
	public AppTaskState State => GetCurrentSnapshot().State;

	/// <summary>
	/// Gets the time when this task was created.
	/// </summary>
	public DateTimeOffset StartTime => GetCurrentSnapshot().StartTime;

	/// <summary>
	/// Gets the time when this task reached an ending state, such as <see cref="AppTaskState.Completed"/> or <see cref="AppTaskState.Error"/>.
	/// </summary>
	public DateTimeOffset? EndTime => GetCurrentSnapshot().EndTime;

	/// <summary>
	/// Gets the automatically generated unique identifier for this task.
	/// </summary>
	[ContractVersion(typeof(AppTaskContract), 131072U)]
	public string Id => _id;

	/// <summary>
	/// Gets a value that indicates whether the user has hidden this task through the Windows Shell.
	/// </summary>
	[ContractVersion(typeof(AppTaskContract), 131072U)]
	public bool HiddenByUser => GetCurrentSnapshot().HiddenByUser;

	/// <summary>
	/// Gets a value that indicates whether the app task feature is supported on the current device.
	/// </summary>
	/// <returns><c>true</c> if app tasks are supported; otherwise, <c>false</c>.</returns>
	public static bool IsSupported() => AppTaskInfoRegistry.IsSupported();

	/// <summary>
	/// Returns all app tasks that were created by the current application.
	/// </summary>
	/// <returns>All tasks created by the app that have not been removed.</returns>
	public static AppTaskInfo[] FindAll() => AppTaskInfoRegistry.FindAll();

	/// <summary>
	/// Creates a new app task with the specified parameters.
	/// </summary>
	/// <param name="title">The required title used to group related tasks.</param>
	/// <param name="subtitle">An optional subtitle that provides additional context. This value can be an empty string.</param>
	/// <param name="deepLink">A URI that launches the app in the context of this task.</param>
	/// <param name="iconUri">The path to an icon that represents the task.</param>
	/// <param name="content">The initial content to display for this task.</param>
	/// <returns>A new object that represents the task.</returns>
	public static AppTaskInfo Create(
		string title,
		string subtitle,
		Uri deepLink,
		Uri iconUri,
		AppTaskContent content)
	{
		ValidateTitle(title);
		ArgumentNullException.ThrowIfNull(subtitle);
		AppTaskValidation.RequireAbsoluteUri(deepLink, nameof(deepLink));
		AppTaskValidation.RequireAbsoluteUri(iconUri, nameof(iconUri));
		ArgumentNullException.ThrowIfNull(content);

		return AppTaskInfoRegistry.Create(title, subtitle, deepLink, iconUri, content.CreateSnapshot());
	}

	/// <summary>
	/// Removes this task from the Shell, but doesn't change its state.
	/// </summary>
	public void Remove()
	{
		if (AppTaskInfoRegistry.Remove(Id) is { } removed)
		{
			UpdateLastKnownSnapshot(removed);
		}
	}

	/// <summary>
	/// Updates both the state and content of this task.
	/// </summary>
	/// <param name="state">The new state of the task.</param>
	/// <param name="content">The new content of the task.</param>
	public void Update(AppTaskState state, AppTaskContent content)
	{
		ValidateState(state);
		ArgumentNullException.ThrowIfNull(content);
		var contentSnapshot = content.CreateSnapshot();

		UpdateSnapshot(snapshot => snapshot with
		{
			State = state,
			Content = contentSnapshot,
			EndTime = GetUpdatedEndTime(snapshot.EndTime, state),
		});
	}

	/// <summary>
	/// Updates the state of this task without changing its content.
	/// </summary>
	/// <param name="state">The new state of the task.</param>
	public void UpdateState(AppTaskState state)
	{
		ValidateState(state);

		UpdateSnapshot(snapshot => snapshot with
		{
			State = state,
			EndTime = GetUpdatedEndTime(snapshot.EndTime, state),
		});
	}

	/// <summary>
	/// Updates the title and subtitle of this task.
	/// </summary>
	/// <param name="title">The new required title.</param>
	/// <param name="subtitle">The new optional subtitle. This value can be an empty string.</param>
	public void UpdateTitles(string title, string subtitle)
	{
		ValidateTitle(title);
		ArgumentNullException.ThrowIfNull(subtitle);
		UpdateSnapshot(snapshot => snapshot with { Title = title, Subtitle = subtitle });
	}

	/// <summary>
	/// Gets the sequence of steps that have been completed for this task.
	/// </summary>
	/// <returns>The completed task steps, or an empty array if the task doesn't use sequence content.</returns>
	public string[] GetCompletedSteps() => (string[])GetCurrentSnapshot().Content.CompletedSteps.Clone();

	/// <summary>
	/// Gets the step that is currently executing for this task.
	/// </summary>
	/// <returns>The executing step, or an empty string if the task doesn't use step-based content.</returns>
	public string GetExecutingStep() => GetCurrentSnapshot().Content.ExecutingStep;

	/// <summary>
	/// Updates the deep link URI for this task.
	/// </summary>
	/// <param name="deepLink">The new URI launched when the user clicks on the task representation.</param>
	[ContractVersion(typeof(AppTaskContract), 131072U)]
	public void UpdateDeepLink(Uri deepLink)
	{
		UpdateSnapshot(snapshot => snapshot with
		{
			DeepLink = AppTaskValidation.RequireAbsoluteUri(deepLink, nameof(deepLink)),
		});
	}

	private static void ValidateTitle(string title)
	{
		ArgumentNullException.ThrowIfNull(title);

		if (string.IsNullOrEmpty(title))
		{
			throw new ArgumentException("The task title is required.", nameof(title));
		}
	}

	private static void ValidateState(AppTaskState state)
	{
		if (state is < AppTaskState.Running or > AppTaskState.Error)
		{
			throw new ArgumentOutOfRangeException(nameof(state));
		}
	}

	private static DateTimeOffset? GetUpdatedEndTime(DateTimeOffset? currentEndTime, AppTaskState state) =>
		currentEndTime ?? (state is AppTaskState.Completed or AppTaskState.Error ? DateTimeOffset.UtcNow : null);

	private AppTaskInfoSnapshot GetCurrentSnapshot()
	{
		if (AppTaskInfoRegistry.TryGet(Id) is { } current)
		{
			UpdateLastKnownSnapshot(current);
			return current;
		}

		lock (_snapshotGate)
		{
			return _lastKnownSnapshot;
		}
	}

	private void UpdateSnapshot(Func<AppTaskInfoSnapshot, AppTaskInfoSnapshot> update)
	{
		if (AppTaskInfoRegistry.Update(Id, update) is { } current)
		{
			UpdateLastKnownSnapshot(current);
			return;
		}

		lock (_snapshotGate)
		{
			_lastKnownSnapshot = update(_lastKnownSnapshot);
		}
	}

	private void UpdateLastKnownSnapshot(AppTaskInfoSnapshot snapshot)
	{
		lock (_snapshotGate)
		{
			_lastKnownSnapshot = snapshot;
		}
	}
}
