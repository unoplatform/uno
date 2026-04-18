using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Foundation.Extensibility;
using Uno.UI.Shell.Tasks;

namespace Windows.UI.Shell.Tasks;

/// <summary>
/// Represents an app task shown in the Taskbar.
/// </summary>
public sealed class AppTaskInfo
{
	private static IAppTaskInfoExtension _extension;
	private static bool _extensionResolved;

	private string _title;
	private string _subtitle;
	private Uri _deepLink;
	private Uri _iconUri;
	private AppTaskState _state;
	private AppTaskContent _content;
	private DateTimeOffset _startTime;
	private DateTimeOffset? _endTime;

	private AppTaskInfo(
		string title,
		string subtitle,
		Uri deepLink,
		Uri iconUri,
		AppTaskContent content)
	{
		_title = title;
		_subtitle = subtitle;
		_deepLink = deepLink;
		_iconUri = iconUri;
		_content = content;
		_state = AppTaskState.Running;
		_startTime = DateTimeOffset.UtcNow;
	}

	/// <summary>
	/// Gets the title of the task that's displayed in the UI.
	/// </summary>
	public string Title => _title;

	/// <summary>
	/// Gets the subtitle of the task.
	/// </summary>
	public string Subtitle => _subtitle;

	/// <summary>
	/// Gets a URI that will be launched when the user clicks on the task's Shell representation.
	/// </summary>
	public Uri DeepLink => _deepLink;

	/// <summary>
	/// Gets the path to an icon that indicates the type of task that is running.
	/// </summary>
	public Uri IconUri => _iconUri;

	/// <summary>
	/// Gets the current state of the task.
	/// </summary>
	public AppTaskState State => _state;

	/// <summary>
	/// Gets the time when the task was created.
	/// </summary>
	public DateTimeOffset StartTime => _startTime;

	/// <summary>
	/// Gets the time when the task reached an ending state (Completed or Error).
	/// </summary>
	public DateTimeOffset? EndTime => _endTime;

	/// <summary>
	/// Gets the current content of the task.
	/// </summary>
	internal AppTaskContent Content => _content;

	/// <summary>
	/// Indicates whether app tasks are supported on the current platform.
	/// </summary>
	/// <returns><c>true</c> if app tasks are supported; otherwise, <c>false</c>.</returns>
	public static bool IsSupported()
	{
		EnsureExtension();
		return _extension?.IsSupported() ?? false;
	}

	/// <summary>
	/// Retrieves all tasks that the app has created that have not been removed by the user or the app.
	/// </summary>
	/// <returns>An array of all active tasks for the current app.</returns>
	public static AppTaskInfo[] FindAll()
	{
		EnsureExtension();
		return _extension?.FindAll() ?? Array.Empty<AppTaskInfo>();
	}

	/// <summary>
	/// Creates a new app task with the specified parameters.
	/// </summary>
	/// <param name="title">The title of the app task.</param>
	/// <param name="subtitle">The subtitle of the app task.</param>
	/// <param name="deepLink">A URI that allows navigation to the full task view in the app.</param>
	/// <param name="iconUri">The path to an icon that represents the app task.</param>
	/// <param name="content">The content of the app task.</param>
	/// <returns>A new instance of <see cref="AppTaskInfo"/>.</returns>
	public static AppTaskInfo Create(
		string title,
		string subtitle,
		Uri deepLink,
		Uri iconUri,
		AppTaskContent content)
	{
		var task = new AppTaskInfo(
			title ?? throw new ArgumentNullException(nameof(title)),
			subtitle ?? throw new ArgumentNullException(nameof(subtitle)),
			deepLink ?? throw new ArgumentNullException(nameof(deepLink)),
			iconUri ?? throw new ArgumentNullException(nameof(iconUri)),
			content ?? throw new ArgumentNullException(nameof(content)));

		EnsureExtension();
		_extension?.OnTaskCreated(task);

		return task;
	}

	/// <summary>
	/// Removes the task from the taskbar.
	/// </summary>
	public void Remove()
	{
		EnsureExtension();
		_extension?.OnTaskRemoved(this);
	}

	/// <summary>
	/// Updates the state and content of the app task.
	/// </summary>
	/// <param name="state">The new state of the task.</param>
	/// <param name="content">The new content of the task.</param>
	public void Update(AppTaskState state, AppTaskContent content)
	{
		_state = state;
		_content = content ?? throw new ArgumentNullException(nameof(content));
		UpdateEndTime(state);

		EnsureExtension();
		_extension?.OnTaskUpdated(this);
	}

	/// <summary>
	/// Updates the state of the app task.
	/// </summary>
	/// <param name="state">The new state of the task.</param>
	public void UpdateState(AppTaskState state)
	{
		_state = state;
		UpdateEndTime(state);

		EnsureExtension();
		_extension?.OnTaskUpdated(this);
	}

	/// <summary>
	/// Updates the title and subtitle of the app task.
	/// </summary>
	/// <param name="title">The new title of the app task.</param>
	/// <param name="subtitle">The new subtitle of the app task.</param>
	public void UpdateTitles(string title, string subtitle)
	{
		_title = title ?? throw new ArgumentNullException(nameof(title));
		_subtitle = subtitle ?? throw new ArgumentNullException(nameof(subtitle));

		EnsureExtension();
		_extension?.OnTaskUpdated(this);
	}

	/// <summary>
	/// Retrieves the completed steps from the current content, if the content represents a sequence of steps.
	/// </summary>
	/// <returns>An array of completed step descriptions.</returns>
	public string[] GetCompletedSteps()
	{
		return _content?.CompletedSteps ?? Array.Empty<string>();
	}

	/// <summary>
	/// Retrieves the currently executing step, if the content represents a sequence of steps.
	/// </summary>
	/// <returns>The description of the currently executing step.</returns>
	public string GetExecutingStep()
	{
		return _content?.ExecutingStep;
	}

	private void UpdateEndTime(AppTaskState state)
	{
		if (state is AppTaskState.Completed or AppTaskState.Error)
		{
			_endTime ??= DateTimeOffset.UtcNow;
		}
	}

	private static void EnsureExtension()
	{
		if (!_extensionResolved)
		{
			ApiExtensibility.CreateInstance<IAppTaskInfoExtension>(null, out _extension);
			_extensionResolved = true;
		}
	}
}
