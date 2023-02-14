using Uno;
using Windows.System;

namespace Microsoft.UI.Xaml.Controls
{
	internal static class PlatformHelpers
	{
		//TODO Uno: Should be implemented for proper keyboard handling.
		[NotImplemented]
		public static VirtualKeyModifiers GetKeyboardModifiers() => VirtualKeyModifiers.None;

		public static void RequestInteractionSoundForElement(ElementSoundKind soundToPlay, DependencyObject element) =>
			ElementSoundPlayer.RequestInteractionSoundForElement(soundToPlay, element);
	}
}
