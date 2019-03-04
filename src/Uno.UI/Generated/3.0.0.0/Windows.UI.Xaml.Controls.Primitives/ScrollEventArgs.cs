#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls.Primitives
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class ScrollEventArgs : global::Windows.UI.Xaml.RoutedEventArgs
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  double NewValue
		{
			get
			{
				throw new global::System.NotImplementedException("The member double ScrollEventArgs.NewValue is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public  global::Windows.UI.Xaml.Controls.Primitives.ScrollEventType ScrollEventType
		{
			get
			{
				throw new global::System.NotImplementedException("The member ScrollEventType ScrollEventArgs.ScrollEventType is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __MACOS__
		[global::Uno.NotImplemented]
		public ScrollEventArgs() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.Primitives.ScrollEventArgs", "ScrollEventArgs.ScrollEventArgs()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollEventArgs.ScrollEventArgs()
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollEventArgs.NewValue.get
		// Forced skipping of method Windows.UI.Xaml.Controls.Primitives.ScrollEventArgs.ScrollEventType.get
	}
}
