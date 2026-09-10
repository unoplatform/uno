#nullable enable
#pragma warning disable CS8305

using System.Linq;
using System.Threading.Tasks;
using Uno.Foundation.Extensibility;
using Uno.UI.Dispatching;
using Uno.UI.Shell.Tasks;
using Windows.UI.Notifications;
using Windows.UI.Shell.Tasks;

namespace Uno.UI.Runtime.Skia.MacOS;

internal sealed class MacOSAppTaskInfoExtension : AppTaskInfoExtensionBase
{
	private static readonly MacOSAppTaskInfoExtension Instance = new();

	private MacOSAppTaskInfoExtension()
	{
	}

	public static void Register() =>
		ApiExtensibility.Register(typeof(IAppTaskInfoExtension), _ => Instance);

	public override bool IsSupported() => true;

	protected override Task OnSynchronizeAsync(AppTaskInfoSnapshot[] tasks)
	{
		var visibleTaskCount = tasks.Count(static task =>
			task.State is AppTaskState.Running
				or AppTaskState.Paused
				or AppTaskState.NeedsAttention
				or AppTaskState.Error);

		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		NativeDispatcher.Main.Enqueue(() =>
		{
			try
			{
				BadgeUpdater.SetAppTaskBadge(visibleTaskCount == 0 ? null : visibleTaskCount);
				completion.SetResult();
			}
			catch (Exception error)
			{
				completion.SetException(error);
			}
		});
		return completion.Task;
	}
}
