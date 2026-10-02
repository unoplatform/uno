#nullable enable
#pragma warning disable CS8305

using System.Linq;
using System.Threading.Tasks;
using Uno.UI.Shell.Tasks;
using Windows.UI.Notifications;

namespace Windows.UI.Shell.Tasks;

internal static partial class AppTaskInfoPlatform
{
	internal static partial IAppTaskInfoExtension? CreateExtension() => WebAssemblyAppTaskInfoExtension.Instance;
}

internal sealed class WebAssemblyAppTaskInfoExtension : AppTaskInfoExtensionBase
{
	internal static WebAssemblyAppTaskInfoExtension Instance { get; } = new();

	private WebAssemblyAppTaskInfoExtension()
	{
	}

	public override bool IsSupported() => __Windows.UI.Notifications.BadgeUpdater.NativeMethods.IsSupported();

	protected override Task OnSynchronizeAsync(AppTaskInfoSnapshot[] tasks)
	{
		var visibleTaskCount = tasks.Count(static task =>
			task.State is AppTaskState.Running
				or AppTaskState.Paused
				or AppTaskState.NeedsAttention
				or AppTaskState.Error);

		BadgeUpdater.SetAppTaskBadge(visibleTaskCount == 0 ? null : visibleTaskCount);
		return Task.CompletedTask;
	}
}
