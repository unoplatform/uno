#if !__TVOS__
#nullable enable

using System;
using System.Collections.Generic;
using Foundation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ObjCRuntime;
using UIKit;
using Uno.UI.Helpers.WinUI;
using Uno.UI.Xaml.Controls.Extensions;

namespace Uno.UI.Runtime.AppleUIKit.Controls;

/// <summary>
/// The iOS text services added to the TextBox touch selection flyout, as UIKit's own edit menu offers them:
/// Look Up, Search Web and Share for a selection, Scan Text (Live Text) at a caret.
/// </summary>
internal sealed class AppleUIKitTextFlyoutCommands : ITextCommandBarFlyoutCommandsExtension
{
	private static readonly Selector s_captureTextFromCamera = new("captureTextFromCamera:");

	public static AppleUIKitTextFlyoutCommands Instance { get; } = new();

	private AppleUIKitTextFlyoutCommands()
	{
	}

	public IEnumerable<TextFlyoutPlatformCommand> GetCommands(TextBox target)
	{
		var selectedText = target.SelectedText;

		if (!string.IsNullOrWhiteSpace(selectedText))
		{
			var term = selectedText.Trim();

			yield return new("LookUp", GetLabel(ResourceAccessor.SR_TextCommandLabelLookUp), () => LookUp(target, term));
			yield return new("SearchWeb", GetLabel(ResourceAccessor.SR_TextCommandLabelSearchWeb), () => SearchWeb(term));
			yield return new("Share", new StandardUICommand(StandardUICommandKind.Share).Label, () => Share(target, selectedText));
		}
		else if (!target.IsReadOnly
			&& OperatingSystem.IsIOSVersionAtLeast(15)
			&& GetProxy(target) is { } proxy
			&& proxy.CanPerform(s_captureTextFromCamera, null))
		{
			// The camera's text goes into the proxy (the first responder) and reaches the TextBox like typed text.
			yield return new("ScanText", GetLabel(ResourceAccessor.SR_TextCommandLabelScanText), () => proxy.CaptureTextFromCamera(null));
		}
	}

	private static string GetLabel(string resourceId) => ResourceAccessor.GetLocalizedStringResource(resourceId);

	private static void LookUp(TextBox target, string term)
		=> Present(target, new UIReferenceLibraryViewController(term));

	// x-web-search: opens Safari on the user's default search engine, which is what the native Search Web does.
	private static void SearchWeb(string term)
	{
		if (NSUrl.FromString("x-web-search://?" + Uri.EscapeDataString(term)) is { } url)
		{
			UIApplication.SharedApplication.OpenUrl(url, new UIApplicationOpenUrlOptions(), null);
		}
	}

	private static void Share(TextBox target, string text)
		=> Present(target, new UIActivityViewController(new NSObject[] { new NSString(text) }, null));

	private static UIView? GetProxy(TextBox target)
		=> (target.Core.TextBoxView?.Extension as InvisibleTextBoxViewExtension)?.NativeView;

	// Presents over the topmost view controller. On iPad it is a popover pointing at the selection; on iPhone UIKit
	// adapts it to a sheet.
	private static void Present(TextBox target, UIViewController controller)
	{
		if (target.XamlRoot is not { } xamlRoot
			|| InvisibleTextBoxViewExtension.GetOverlayLayer(xamlRoot) is not { } layer
			|| layer.Window?.RootViewController is not { } presenter)
		{
			return;
		}

		while (presenter.PresentedViewController is { } presented)
		{
			presenter = presented;
		}

		controller.ModalPresentationStyle = UIModalPresentationStyle.Popover;
		if (controller.PopoverPresentationController is { } popover)
		{
			// A popover with no source makes UIKit throw on iPad, so fall back to the middle of the layer.
			popover.SourceView = layer;
			popover.SourceRect = target.Core.TryGetSelectionBoundsInRoot(out var bounds)
				? new CoreGraphics.CGRect(bounds.X, bounds.Y, bounds.Width, bounds.Height)
				: new CoreGraphics.CGRect(layer.Bounds.Width / 2, layer.Bounds.Height / 2, 0, 0);
		}

		presenter.PresentViewController(controller, animated: true, completionHandler: null);
	}
}
#endif
