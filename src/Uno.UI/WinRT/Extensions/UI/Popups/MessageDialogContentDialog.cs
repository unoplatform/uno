using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Helpers.WinUI;
using Windows.System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace Uno.UI.WinRT.Extensions.UI.Popups;

internal class MessageDialogContentDialog : ContentDialog
{
	private readonly MessageDialog _messageDialog;
	private readonly List<IUICommand> _commands;
	private readonly uint _cancelCommandIndex;

	public MessageDialogContentDialog(MessageDialog messageDialog)
	{
		DefaultStyleKey = typeof(ContentDialog);
		_messageDialog = messageDialog ?? throw new ArgumentNullException(nameof(messageDialog));

		if (Application.Current.Resources.ContainsKey("DefaultContentDialogStyle"))
		{
			Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];
		}

		_commands = _messageDialog.Commands.ToList();
		_cancelCommandIndex = _messageDialog.CancelCommandIndex;

		Content = _messageDialog.Content;
		Title = _messageDialog.Title;

		if (_commands.Count == 0)
		{
			// Only show a close button
			var closeText = ResourceAccessor.GetLocalizedStringResource("NavigationCloseButtonName");
			_commands.Add(new UICommand(closeText));
		}

		PrimaryButtonText = _commands[0].Label;
		SecondaryButtonText = _commands.Count > 1 ? _commands[1].Label : null;
		CloseButtonText = _commands.Count > 2 ? _commands[2].Label : null;

		DefaultButton = (ContentDialogButton)(_messageDialog.DefaultCommandIndex + 1); // ContentDialogButton indexed from 1
	}

	public async Task<IUICommand> ShowAsync(CancellationToken ct)
	{
		var result = await base.ShowAsync().AsTask(ct);

		if (result == ContentDialogResult.Primary)
		{
			_commands[0].Invoked?.Invoke(_commands[0]);
			return _commands[0];
		}
		else if (result == ContentDialogResult.Secondary)
		{
			_commands[1].Invoked?.Invoke(_commands[1]);
			return _commands[1];
		}
		else
		{
			// In case of task cancellation, the "third" button
			// might not really exist - use last instead.
			var lastCommand = _commands.Last();
			lastCommand.Invoked?.Invoke(lastCommand);
			return lastCommand;
		}
	}

	private protected override void OnPopupKeyDown(object sender, KeyRoutedEventArgs e)
	{
		// Escape should not apply on MessageDialog.
		if (e.Key != VirtualKey.Escape)
		{
			base.OnPopupKeyDown(sender, e);
		}
		else
		{
			if (_messageDialog.CancelCommandIndex < _commands.Count)
			{
				var contentDialogResult = ContentDialogResult.None;
				if (_messageDialog.CancelCommandIndex != 2) // Index 2 matches the close button - result of None.
				{
					contentDialogResult = (ContentDialogResult)(_messageDialog.CancelCommandIndex + 1);
				}
				Hide(contentDialogResult);
			}
		}
	}
}
