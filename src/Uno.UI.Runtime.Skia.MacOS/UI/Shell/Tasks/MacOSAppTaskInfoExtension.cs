using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Uno.Foundation.Extensibility;
using Uno.UI.Shell.Tasks;
using Windows.UI.Shell.Tasks;

namespace Uno.UI.Runtime.Skia.MacOS;

/// <summary>
/// macOS implementation of <see cref="IAppTaskInfoExtension"/> that maps
/// app task state to the macOS Dock tile badge via NSDockTile.
/// </summary>
/// <remarks>
/// macOS does not have a taskbar task list equivalent. This implementation:
/// - Tracks tasks in memory
/// - Shows the number of running tasks as a badge on the Dock icon
/// - Clears the badge when no tasks are active
/// </remarks>
internal sealed class MacOSAppTaskInfoExtension : IAppTaskInfoExtension
{
	private static readonly MacOSAppTaskInfoExtension _instance = new();

	private readonly List<AppTaskInfo> _tasks = new();
	private readonly object _gate = new();

	private MacOSAppTaskInfoExtension()
	{
	}

	public static void Register() =>
		ApiExtensibility.Register(typeof(IAppTaskInfoExtension), _ => _instance);

	public bool IsSupported() => true;

	public AppTaskInfo[] FindAll()
	{
		lock (_gate)
		{
			return _tasks.ToArray();
		}
	}

	public void OnTaskCreated(AppTaskInfo task)
	{
		lock (_gate)
		{
			_tasks.Add(task);
		}

		UpdateDockBadge();
	}

	public void OnTaskUpdated(AppTaskInfo task)
	{
		UpdateDockBadge();
	}

	public void OnTaskRemoved(AppTaskInfo task)
	{
		lock (_gate)
		{
			_tasks.Remove(task);
		}

		UpdateDockBadge();
	}

	private void UpdateDockBadge()
	{
		int runningCount;
		lock (_gate)
		{
			runningCount = _tasks.Count(t => t.State == AppTaskState.Running);
		}

		// Show running task count as badge on the Dock icon, or clear it.
		var badge = runningCount > 0
			? runningCount.ToString(CultureInfo.InvariantCulture)
			: string.Empty;

		NativeUno.uno_application_set_badge(badge);
	}
}
