using System;
using UIKit;
#if !__TVOS__
using CoreGraphics;
using Uno.Extensions;
#endif

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

/// <summary>
/// Builds the toolbar shown above the soft keyboard when
/// <see cref="Uno.UI.Xaml.Controls.TextBoxExtensions.ShowKeyboardDismissButtonProperty"/> is set, giving
/// the user a way out of inputs the Enter key cannot dismiss (a multiline TextBox, mainly).
/// </summary>
internal static class KeyboardDismissAccessory
{
#if __TVOS__
	// UIToolbar is not part of tvOS - and neither is a soft keyboard to attach an accessory to.
	internal static UIView? TryCreate(WeakReference<InvisibleTextBoxViewExtension> extension) => null;
#else
	private const int ToolbarHeight = 44;

	internal static UIView? TryCreate(WeakReference<InvisibleTextBoxViewExtension> extension)
	{
		var toolbar = new UIToolbar(new CGRect(0, 0, UIScreen.MainScreen.Bounds.Width, ToolbarHeight))
		{
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth
		};

		// The system item supplies the localized title and the accessibility label.
		var done = new UIBarButtonItem(UIBarButtonSystemItem.Done, (_, _) => extension.GetTarget()?.DismissKeyboard());

		toolbar.Items = [new UIBarButtonItem(UIBarButtonSystemItem.FlexibleSpace), done];
		toolbar.SizeToFit();

		return toolbar;
	}
#endif
}
