using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Popups.Internal;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.WinRT.Extensions.UI.Popups;

/// <summary>
/// Provides a ContentDialog-based implementation of MessageDialog.
/// </summary>
internal class MessageDialogExtension : IMessageDialogExtension
{
	private readonly MessageDialog _messageDialog;

	public MessageDialogExtension(MessageDialog messageDialog)
	{
		_messageDialog = messageDialog ?? throw new ArgumentNullException(nameof(messageDialog));
	}

	public async Task<IUICommand> ShowAsync(CancellationToken ct)
	{
		var contentDialog = new MessageDialogContentDialog(_messageDialog);
		if (_messageDialog.AssociatedWindow is Windows.UI.Xaml.Window window &&
			window.RootElement?.XamlRoot is { } xamlRoot)
		{
			contentDialog.XamlRoot = xamlRoot;
		}
		return await contentDialog.ShowAsync(ct);
	}
}
