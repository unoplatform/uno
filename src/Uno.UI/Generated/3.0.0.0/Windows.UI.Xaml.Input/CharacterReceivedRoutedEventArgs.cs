#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Input
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class CharacterReceivedRoutedEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  bool Handled
		{
			get
			{
				throw new global::System.NotImplementedException("The member bool CharacterReceivedRoutedEventArgs.Handled is not implemented in Uno.");
			}
			set
			{
				global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Input.CharacterReceivedRoutedEventArgs", "bool CharacterReceivedRoutedEventArgs.Handled");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  char Character
		{
			get
			{
				throw new global::System.NotImplementedException("The member char CharacterReceivedRoutedEventArgs.Character is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Core.CorePhysicalKeyStatus KeyStatus
		{
			get
			{
				throw new global::System.NotImplementedException("The member CorePhysicalKeyStatus CharacterReceivedRoutedEventArgs.KeyStatus is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Input.CharacterReceivedRoutedEventArgs.Character.get
		// Forced skipping of method Windows.UI.Xaml.Input.CharacterReceivedRoutedEventArgs.KeyStatus.get
		// Forced skipping of method Windows.UI.Xaml.Input.CharacterReceivedRoutedEventArgs.Handled.get
		// Forced skipping of method Windows.UI.Xaml.Input.CharacterReceivedRoutedEventArgs.Handled.set
	}
}
