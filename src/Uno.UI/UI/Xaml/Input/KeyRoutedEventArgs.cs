using Uno.UI.Xaml.Input;
using Windows.Foundation.Metadata;
using Windows.System;
using Windows.UI.Core;

namespace Windows.UI.Xaml.Input
{
	public partial class KeyRoutedEventArgs : RoutedEventArgs, IHandleableRoutedEventArgs
	{
		private readonly CorePhysicalKeyStatus? _keyStatus;

		internal KeyRoutedEventArgs(object originalSource, VirtualKey key, VirtualKeyModifiers modifiers, CorePhysicalKeyStatus? keyStatus = null, char? unicodeKey = null)
			: base(originalSource)
		{
			Key = key;
			OriginalKey = key;
			KeyboardModifiers = modifiers;
			_keyStatus = keyStatus;
			UnicodeKey = unicodeKey ?? MapToChar(key, modifiers);
		}

		public bool Handled { get; set; }

		public VirtualKey Key { get; }

#if __ANDROID__ || __IOS__ || IS_UNIT_TESTS || __WASM__ || false || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "IS_UNIT_TESTS", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
#endif
		public CorePhysicalKeyStatus KeyStatus
		{
			get
			{
				if (_keyStatus == null)
				{
					ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.KeyRoutedEventArgs", "KeyStatus");
					return default;
				}
				else
				{
					return _keyStatus.Value;
				}
			}
		}

		public global::Windows.System.VirtualKey OriginalKey { get; }

		internal VirtualKeyModifiers KeyboardModifiers { get; }

		internal bool HandledShouldNotImpedeTextInput { get; set; }

		/// <summary>
		/// This gets the the Unicode Key associated with the event. This is not limited to the
		/// VirtualKey options. Currently, this is implemented for Skia and pretty much approximated on other platforms.
		/// </summary>
		internal char? UnicodeKey { get; }

		private static char? MapToChar(VirtualKey key, VirtualKeyModifiers modifiers)
		{
			return (key, modifiers) switch
			{
				(VirtualKey.Space, VirtualKeyModifiers.None) => ' ',
				( >= VirtualKey.Number0 and <= VirtualKey.Number9, VirtualKeyModifiers.None) => (char)key,
				( >= VirtualKey.A and <= VirtualKey.Z, VirtualKeyModifiers.None) => char.ToLowerInvariant((char)key),
				( >= VirtualKey.A and <= VirtualKey.Z, VirtualKeyModifiers.Shift) => (char)key,
				(VirtualKey.Back, VirtualKeyModifiers.None) => (char)key,
				_ => null,
			};
		}
	}
}
