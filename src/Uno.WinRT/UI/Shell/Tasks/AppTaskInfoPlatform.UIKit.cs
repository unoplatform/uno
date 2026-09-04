#nullable enable
#pragma warning disable CS8305

using System;
using System.Linq;
using System.Threading.Tasks;
using UIKit;
using Uno.UI.Shell.Tasks;
using Windows.UI.Notifications;

namespace Windows.UI.Shell.Tasks;

internal static partial class AppTaskInfoPlatform
{
	internal static partial IAppTaskInfoExtension? CreateExtension() => AppleAppTaskInfoExtension.Instance;
}

internal sealed class AppleAppTaskInfoExtension : AppTaskInfoExtensionBase
{
	internal static AppleAppTaskInfoExtension Instance { get; } = new();

	private AppleAppTaskInfoExtension()
	{
	}

	public override bool IsSupported() => true;

	protected override Task OnSynchronizeAsync(AppTaskInfoSnapshot[] tasks)
	{
		var visibleTaskCount = tasks.Count(static task =>
			task.State is AppTaskState.Running
				or AppTaskState.Paused
				or AppTaskState.NeedsAttention
				or AppTaskState.Error);

		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
		{
			try
			{
				UpdateBadge(visibleTaskCount);
				completion.SetResult();
			}
			catch (Exception error)
			{
				completion.SetException(error);
			}
		});
		return completion.Task;
	}

	private void UpdateBadge(int visibleTaskCount)
		=> BadgeUpdater.SetAppTaskBadge(visibleTaskCount == 0 ? null : visibleTaskCount);
}
