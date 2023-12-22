using Windows.System;
using Uno.UI.Xaml.Input;
using Windows.UI.Core;
using Windows.Foundation.Metadata;

namespace Microsoft.UI.Xaml.Input
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
			UnicodeKey = unicodeKey;
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
					ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Input.KeyRoutedEventArgs", "KeyStatus");
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

		/// <summary>
		/// This gets the the Unicode Key associated with the event. This is not limited to the
		/// VirtualKey options. Currently, this is only implemented for skia.
		/// </summary>
		internal char? UnicodeKey { get; }
	}
}
