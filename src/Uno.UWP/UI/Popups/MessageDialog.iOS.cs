#if XAMARIN_IOS
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UIKit;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.Popups
{
	public partial class MessageDialog
	{
		private static readonly SemaphoreSlim _viewControllerAccess = new SemaphoreSlim(1, 1);

		private async void ShowInner(CancellationToken ct, TaskCompletionSource<IUICommand> invokedCommand)
		{
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
							invokedCommand.TrySetResult(command);
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

			using (ct.Register(() =>
				{
					// If the cancellation token itself gets cancelled, we cancel as well.
					invokedCommand.TrySetCanceled();
					UIApplication.SharedApplication.KeyWindow?.RootViewController?.DismissViewController(false, () => { });
				}))
			{
				await _viewControllerAccess.WaitAsync(ct);

				try
				{
					try
					{
						await UIApplication.SharedApplication.KeyWindow?.RootViewController?.PresentViewControllerAsync(alertController, animated: true);
					}
					catch (Exception error)
					{
						invokedCommand.TrySetException(error);
					}
				}
				finally
				{
					_viewControllerAccess.Release();
				}
			}
		}

		partial void ValidateCommands()
		{
			// On iOS, there is no limit. The items will be in a scrollable list.
		}
	}
}
#endif
