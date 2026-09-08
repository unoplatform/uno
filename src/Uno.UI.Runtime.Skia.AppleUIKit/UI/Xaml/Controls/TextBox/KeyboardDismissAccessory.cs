using System;
using UIKit;
#if !__TVOS__
using CoreGraphics;
using Foundation;
using Uno.Extensions;
#endif

namespace Uno.WinUI.Runtime.Skia.AppleUIKit.Controls;

/// <summary>
/// Builds the bar shown above the soft keyboard when
/// <see cref="Uno.UI.Xaml.Controls.TextBoxExtensions.ShowKeyboardDismissButtonProperty"/> is set, giving
/// the user a way out of inputs the Enter key cannot dismiss (a multiline TextBox, mainly).
/// </summary>
internal static class KeyboardDismissAccessory
{
#if __TVOS__
	// tvOS has no soft keyboard to attach an accessory to.
	internal static UIView? TryCreate(WeakReference<InvisibleTextBoxViewExtension> extension) => null;
#else
	private const int BarHeight = 44;
	private const int HorizontalMargin = 16;
	private const int MinimumTouchTarget = 44;

	// A plain UIView rather than a UIToolbar: from iOS 26 on a toolbar renders its items as floating
	// Liquid Glass controls - a system Done item becomes a checkmark glyph, a titled one a filled
	// capsule - instead of the flat bar with a "Done" label this feature is meant to provide.
	internal static UIView? TryCreate(WeakReference<InvisibleTextBoxViewExtension> extension)
	{
		var width = UIScreen.MainScreen.Bounds.Width;

		var bar = new UIView(new CGRect(0, 0, width, BarHeight))
		{
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth,
			BackgroundColor = UIColor.SecondarySystemBackground
		};

		var separator = new UIView(new CGRect(0, 0, width, 1 / UIScreen.MainScreen.Scale))
		{
			AutoresizingMask = UIViewAutoresizing.FlexibleWidth,
			BackgroundColor = UIColor.Separator
		};
		bar.AddSubview(separator);

		var done = UIButton.FromType(UIButtonType.System);
		done.SetTitle(GetDoneTitle(), UIControlState.Normal);
		done.TitleLabel.Font = UIFont.SystemFontOfSize(17, UIFontWeight.Semibold);
		done.SizeToFit();

		var doneWidth = (nfloat)Math.Max(done.Frame.Width, MinimumTouchTarget);
		done.Frame = new CGRect(width - doneWidth - HorizontalMargin, 0, doneWidth, BarHeight);
		done.AutoresizingMask = UIViewAutoresizing.FlexibleLeftMargin;
		done.TouchUpInside += (_, _) => extension.GetTarget()?.DismissKeyboard();
		bar.AddSubview(done);

		return bar;
	}

	// Borrowed from UIKit's own translations, so the button follows the system language without Uno
	// shipping a string of its own.
	private static string GetDoneTitle()
	{
		var uiKit = NSBundle.FromIdentifier("com.apple.UIKit");
		var title = uiKit?.GetLocalizedString("Done", "Done", null);

		return string.IsNullOrEmpty(title) ? "Done" : title;
	}
#endif
}
