#if __MACOS__
using System.Collections.Generic;
using System.Linq;
using AppKit;
using Windows.Foundation;

namespace Windows.UI.Popups
{
	public partial class MessageDialog
	{
		private const int FirstButtonResultIndex = 1000;  // Result is a number starting at 1000

		public IAsyncOperation<IUICommand> ShowAsync()
		{
			var alert = new NSAlert()
			{
				MessageText = Title ?? "",
				InformativeText = Content ?? "",
				AlertStyle = NSAlertStyle.Informational
			};

			var actualCommandOrder = new List<UICommand>();

			UICommand defaultCommand = null;
			if (DefaultCommandIndex >= 0 &&
				DefaultCommandIndex < Commands.Count)
			{
				defaultCommand = Commands[(int)DefaultCommandIndex] as UICommand;				
			}

			// Default command must be added first (to be default on macOS).
			if ( defaultCommand != null)
			{
				alert.AddButton(defaultCommand.Label);
				actualCommandOrder.Add(defaultCommand);
			}
			
			// Add remaining alert buttons in reverse because NSAlert.AddButtons adds them
			// from the right to the left.
			foreach (var command in Commands.OfType<UICommand>())
			{
				if (command == defaultCommand)
				{
					// Skip, already added.
					continue;
				}

				alert.AddButton(command.Label);
				actualCommandOrder.Add(command);
			}

			return AsyncOperation.FromTask<IUICommand>(async ct =>
			{
				var response = await alert.BeginSheetAsync(NSApplication.SharedApplication.KeyWindow);

				if (actualCommandOrder.Count == 0)
				{
					// There is no button specified, return dummy OK result.
					return new UICommand("OK");
				}

				var commandIndex = (int)response - FirstButtonResultIndex;
				var commandResponse = actualCommandOrder[commandIndex];
				commandResponse?.Invoked?.Invoke(commandResponse);
				return commandResponse;
			});
		}
	}
}
#endif
