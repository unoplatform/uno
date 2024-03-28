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

internal partial class MessageDialogContentDialog : ContentDialog
{
	private readonly MessageDialog _messageDialog;
	private readonly List<IUICommand> _commands;
	private readonly uint _cancelCommandIndex;

	public MessageDialogContentDialog(MessageDialog messageDialog)
	{
		DefaultStyleKey = typeof(ContentDialog);
		_messageDialog = messageDialog ?? throw new ArgumentNullException(nameof(messageDialog));

		var styleOverriden = TryApplyStyle(WinRTFeatureConfiguration.MessageDialog.StyleOverride);
		if (!styleOverriden)
		{
			// WinUI provides a modern style for ContentDialog, which is not applied automatically.
			// Force apply it if available.
			TryApplyStyle("DefaultContentDialogStyle");
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

	private bool TryApplyStyle(string resourceKey)
	{
		if (!string.IsNullOrEmpty(resourceKey) &&
			Application.Current.Resources.TryGetValue(resourceKey, out var resource) &&
			resource is Style style)
		{
			Style = style;
			return true;
		}

		return false;
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
			// Always use the "last" command as fallback
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
