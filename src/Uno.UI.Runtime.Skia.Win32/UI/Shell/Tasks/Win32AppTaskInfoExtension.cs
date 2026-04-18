using System;
using System.Collections.Generic;
using System.Linq;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.UI.Shell.Tasks;
using Windows.UI.Shell.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Win32 implementation of <see cref="IAppTaskInfoExtension"/> that maps
/// app task state to Windows taskbar progress indication via ITaskbarList3.
/// </summary>
internal sealed class Win32AppTaskInfoExtension : IAppTaskInfoExtension
{
	private static readonly Win32AppTaskInfoExtension _instance = new();

	private readonly List<AppTaskInfo> _tasks = new();
	private readonly object _gate = new();

	private Win32AppTaskInfoExtension()
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

		UpdateTaskbarProgress();
	}

	public void OnTaskUpdated(AppTaskInfo task)
	{
		UpdateTaskbarProgress();
	}

	public void OnTaskRemoved(AppTaskInfo task)
	{
		lock (_gate)
		{
			_tasks.Remove(task);
		}

		UpdateTaskbarProgress();
	}

	private void UpdateTaskbarProgress()
	{
		int runningCount;
		int totalCount;
		bool hasError;
		bool hasPaused;
		bool hasNeedsAttention;

		lock (_gate)
		{
			runningCount = _tasks.Count(t => t.State == AppTaskState.Running);
			totalCount = _tasks.Count;
			hasError = _tasks.Any(t => t.State == AppTaskState.Error);
			hasPaused = _tasks.Any(t => t.State is AppTaskState.Paused or AppTaskState.NeedsAttention);
			hasNeedsAttention = _tasks.Any(t => t.State == AppTaskState.NeedsAttention);
		}

		foreach (var hwnd in Win32WindowWrapper.GetHwnds())
		{
			if (totalCount == 0 || runningCount == 0 && !hasError && !hasPaused && !hasNeedsAttention)
			{
				// No active tasks — clear progress
				TaskBarList.SetProgressState(hwnd, TBPFLAG.TBPF_NOPROGRESS);
			}
			else if (hasError)
			{
				// At least one error — show error state
				TaskBarList.SetProgressState(hwnd, TBPFLAG.TBPF_ERROR);
				TaskBarList.SetProgressValue(hwnd, 100, 100);
			}
			else if (hasPaused)
			{
				// At least one paused — show paused state
				TaskBarList.SetProgressState(hwnd, TBPFLAG.TBPF_PAUSED);
				TaskBarList.SetProgressValue(hwnd, 50, 100);
			}
			else
			{
				// Running tasks — show indeterminate progress
				TaskBarList.SetProgressState(hwnd, TBPFLAG.TBPF_INDETERMINATE);
			}
		}
	}
}
