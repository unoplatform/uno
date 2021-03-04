using Uno;
using Windows.System;
using Windows.UI.Xaml;

namespace Windows.UI.Xaml.Controls
{
	internal static class PlatformHelpers
	{
		[NotImplemented]
		public static VirtualKeyModifiers GetKeyboardModifiers()
		{
			return VirtualKeyModifiers.None;
		}

		public static void RequestInteractionSoundForElement(ElementSoundKind soundToPlay, DependencyObject element)
		{
			ElementSoundPlayer.RequestInteractionSoundForElement(soundToPlay, element);
		}
	}
}
