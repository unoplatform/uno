using Foundation;
using UIKit;
using Windows.Foundation;
using Windows.UI.Core;
using Uno.UI.Runtime.Skia.AppleUIKit.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml;
using System;

namespace Uno.UI.Runtime.Skia.AppleUIKit;

internal sealed class UnoKeyboardInputSource : IUnoKeyboardInputSource
{
	public static UnoKeyboardInputSource Instance { get; } = new();

	private UnoKeyboardInputSource()
	{
	}
#pragma warning disable CS0067
	public event TypedEventHandler<object, KeyEventArgs>? KeyDown;
	public event TypedEventHandler<object, KeyEventArgs>? KeyUp;
#pragma warning restore CS0067

	public bool TryHandlePresses(NSSet<UIPress> presses, UIPressesEvent evt, AppleUIKitWindow window)
	{
		return false;
	}
}
