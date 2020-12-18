using System;
using System.Collections.Generic;
using System.Text;
using Windows.System;
using Uno.UI.Xaml.Input;
using Windows.UI.Core;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Input
{
	public partial class KeyRoutedEventArgs : RoutedEventArgs, ICancellableRoutedEventArgs
	{
		private readonly CorePhysicalKeyStatus? _keyStatus;

		internal KeyRoutedEventArgs(object originalSource, VirtualKey key, CorePhysicalKeyStatus? keyStatus = null)
			: base(originalSource)
		{
			Key = key;
			OriginalKey = key;
			_keyStatus = keyStatus;
		}

		public bool Handled { get; set; }
		public VirtualKey Key { get; }

		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || false || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__NETSTD_REFERENCE__", "__MACOS__")]
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
	}
}
