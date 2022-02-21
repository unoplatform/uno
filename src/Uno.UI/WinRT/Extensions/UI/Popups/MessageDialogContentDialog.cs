using System;
using System.Linq;
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

	public MessageDialogContentDialog(MessageDialog messageDialog)
	{
		DefaultStyleKey = typeof(ContentDialog);
		_messageDialog = messageDialog ?? throw new ArgumentNullException(nameof(messageDialog));
	}

	public new async Task<IUICommand> ShowAsync()
	{
		if (Application.Current.Resources.ContainsKey("DefaultContentDialogStyle"))
		{
			Style = (Style)Application.Current.Resources["DefaultContentDialogStyle"];
		}

		var commands = _messageDialog.Commands.ToList();

		Content = _messageDialog.Content;
		Title = _messageDialog.Title;

		if (commands.Count == 0)
		{
			// Only show a close button
			var closeText = ResourceAccessor.GetLocalizedStringResource("NavigationCloseButtonName");
			commands.Add(new UICommand(closeText));
		}

		PrimaryButtonText = commands[0].Label;
		SecondaryButtonText = commands.Count > 1 ? commands[1].Label : null;
		CloseButtonText = commands.Count > 2 ? commands[2].Label : null;

		DefaultButton = (ContentDialogButton)(_messageDialog.DefaultCommandIndex + 1); // ContentDialogButton indexed from 1

		var result = await base.ShowAsync();
		if (result == ContentDialogResult.Primary)
		{
			commands[0].Invoked?.Invoke(commands[0]);
			return commands[0];
		}
		else if (result == ContentDialogResult.Secondary)
		{
			commands[1].Invoked?.Invoke(commands[1]);
			return commands[1];
		}
		else
		{
			commands[2].Invoked?.Invoke(commands[2]);
			return commands[2];
		}
	}

	private protected override void OnPopupKeyDown(object sender, KeyRoutedEventArgs e)
	{
		// Escape should not apply on MessageDialog.
		if (e.Key != VirtualKey.Escape)
		{
			base.OnPopupKeyDown(sender, e);
		}
	}
}
