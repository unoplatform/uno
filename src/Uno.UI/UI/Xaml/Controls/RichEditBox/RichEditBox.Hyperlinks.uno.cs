#nullable enable

using System;
using System.Globalization;
using System.Threading.Tasks;
using Uno.Foundation.Logging;
using Uno.UI.Helpers.WinUI;

namespace Microsoft.UI.Xaml.Controls;

partial class RichEditBox
{
	internal Func<Uri, Task<bool>>? LinkLauncherForTesting { get; set; }

	internal Func<Uri, Task<bool>>? LinkConfirmationForTesting { get; set; }

	private async Task LaunchLinkAsync(Uri uri)
	{
		try
		{
			await TryInvokeLauncher(uri);
		}
		catch (Exception error) when (error is not OutOfMemoryException and not StackOverflowException and not AccessViolationException)
		{
			typeof(RichEditBox).LogError()?.Error("Failed to launch a RichEditBox hyperlink.", error);
		}
	}

	private async Task LaunchLinkPlatformAsync(Uri uri)
	{
		if (OperatingSystem.IsBrowser() && uri.Scheme == "javascript")
		{
			typeof(RichEditBox).LogWarn()?.Warn("The browser cannot launch a JavaScript URI as an external application.");
			return;
		}

		var launched = LinkLauncherForTesting is { } testLauncher
			? await testLauncher(uri)
			: await global::Windows.System.Launcher.LaunchUriAsync(uri);
		if (!launched)
		{
			typeof(RichEditBox).LogWarn()?.Warn("No handler accepted a RichEditBox hyperlink.");
		}
	}

	private async Task<bool> ConfirmLinkLaunchAsync(Uri uri)
	{
		if (LinkConfirmationForTesting is { } confirmation)
		{
			return await confirmation(uri);
		}

		if (XamlRoot is not { } xamlRoot)
		{
			typeof(RichEditBox).LogWarn()?.Warn("Cannot confirm a RichEditBox hyperlink without an owning XamlRoot.");
			return false;
		}

		var dialog = new ContentDialog
		{
			XamlRoot = xamlRoot,
			Title = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_RichEditBoxOpenLinkTitle),
			Content = string.Format(
				CultureInfo.CurrentCulture,
				ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_RichEditBoxOpenLinkWarning),
				uri.OriginalString),
			PrimaryButtonText = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_RichEditBoxOpenLinkButton),
			CloseButtonText = ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_RichEditBoxCancelLinkButton),
			DefaultButton = ContentDialogButton.Close,
		};
		return await dialog.ShowAsync() == ContentDialogResult.Primary;
	}
}
