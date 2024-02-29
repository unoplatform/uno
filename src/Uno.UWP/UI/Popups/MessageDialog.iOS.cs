using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UIKit;
using Uno.Extensions;
using Uno.Helpers.Theming;
using Windows.ApplicationModel.Core;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.Popups;

public partial class MessageDialog
{
	private static readonly SemaphoreSlim _viewControllerAccess = new SemaphoreSlim(1, 1);

	private async Task<IUICommand> ShowNativeAsync(CancellationToken ct)
	{
		var result = new TaskCompletionSource<IUICommand>();
		var alertActions = Commands
			.Where(command => !(command is UICommandSeparator)) // Not supported on iOS
			.DefaultIfEmpty(new UICommand("OK")) // TODO: Localize (PBI 28711)
			.Select((command, index) => UIAlertAction
				.Create(
					title: command.Label ?? "",
					style: (command as UICommand)?.IsDestructive ?? false
						? UIAlertActionStyle.Destructive
						: UIAlertActionStyle.Default,
					handler: _ =>
					{
						command.Invoked?.Invoke(command);
						result.TrySetResult(command);
					}
				)
			)
			.ToArray();

		var alertController = UIAlertController.Create(
			Title ?? "",
			Content ?? "",
			UIAlertControllerStyle.Alert
		);

		alertActions.ForEach(alertController.AddAction);

		if (UIKit.UIDevice.CurrentDevice.CheckSystemVersion(9, 0))
		{
			alertController.PreferredAction = alertActions.ElementAtOrDefault((int)DefaultCommandIndex);
		}

		// Theme should match the application theme
		alertController.OverrideUserInterfaceStyle = CoreApplication.RequestedTheme == SystemTheme.Light ? UIUserInterfaceStyle.Light : UIUserInterfaceStyle.Dark;

		using (ct.Register(() =>
			{
				// If the cancellation token itself gets cancelled, we cancel as well.
				result.TrySetCanceled();
				UIApplication.SharedApplication.KeyWindow?.RootViewController?.DismissViewController(false, () => { });
			}, useSynchronizationContext: true))
		{
			await _viewControllerAccess.WaitAsync(ct);

			try
			{
				await UIApplication.SharedApplication.KeyWindow?.RootViewController?.PresentViewControllerAsync(alertController, animated: true)!;
			}
			finally
			{
				_viewControllerAccess.Release();
			}

			return await result.Task;
		}
	}
}
