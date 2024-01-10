using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Popups;
using Windows.UI.Popups.Internal;
using Microsoft.UI.Xaml.Controls;

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
		return await contentDialog.ShowAsync(ct);
	}
}
