#nullable enable
#pragma warning disable CS8305

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Uno.UI.Dispatching;
using Uno.UI.Shell.Tasks;
using Windows.UI.Shell.Tasks;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;

namespace Uno.UI.Runtime.Skia.Win32;

internal sealed class Win32AppTaskInfoExtension : AppTaskInfoExtensionBase
{
	private static readonly Win32AppTaskInfoExtension Instance = new();

	private AppTaskInfoSnapshot[] _latestTasks = Array.Empty<AppTaskInfoSnapshot>();

	private Win32AppTaskInfoExtension()
	{
	}

	public static void Register() =>
		ApiExtensibility.Register(typeof(IAppTaskInfoExtension), _ => Instance);

	public override bool IsSupported() => OperatingSystem.IsWindowsVersionAtLeast(6, 1);

	internal static void ApplyToWindow(HWND hwnd) =>
		Instance.UpdateTaskbarProgress(hwnd, Volatile.Read(ref Instance._latestTasks));

	protected override Task OnSynchronizeAsync(AppTaskInfoSnapshot[] tasks)
	{
		Volatile.Write(ref _latestTasks, tasks);
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		Win32EventLoop.Schedule(
			() =>
			{
				try
				{
					foreach (var hwnd in Win32WindowWrapper.GetHwnds())
					{
						UpdateTaskbarProgress(hwnd, tasks);
					}

					completion.SetResult();
				}
				catch (Exception error)
				{
					completion.SetException(error);
				}
			},
			NativeDispatcherPriority.Normal);
		return completion.Task;
	}

	private void UpdateTaskbarProgress(HWND hwnd, AppTaskInfoSnapshot[] tasks)
	{
		var hasError = tasks.Any(static task => task.State == AppTaskState.Error);
		var hasPaused = tasks.Any(static task => task.State is AppTaskState.Paused or AppTaskState.NeedsAttention);
		var hasRunning = tasks.Any(static task => task.State == AppTaskState.Running);

		if (hasError)
		{
			TaskBarList.SetProgressState(hwnd, TBPFLAG.TBPF_ERROR);
			TaskBarList.SetProgressValue(hwnd, completed: 100, total: 100);
		}
		else if (hasPaused)
		{
			TaskBarList.SetProgressState(hwnd, TBPFLAG.TBPF_PAUSED);
			TaskBarList.SetProgressValue(hwnd, completed: 50, total: 100);
		}
		else if (hasRunning)
		{
			TaskBarList.SetProgressState(hwnd, TBPFLAG.TBPF_INDETERMINATE);
		}
		else
		{
			TaskBarList.SetProgressState(hwnd, TBPFLAG.TBPF_NOPROGRESS);
		}
	}
}
