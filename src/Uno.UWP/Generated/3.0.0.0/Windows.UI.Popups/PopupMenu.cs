#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Popups
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class PopupMenu 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::System.Collections.Generic.IList<global::Windows.UI.Popups.IUICommand> Commands
		{
			get
			{
				throw new global::System.NotImplementedException("The member IList<IUICommand> PopupMenu.Commands is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IList%3CIUICommand%3E%20PopupMenu.Commands");
			}
		}
		#endif
		// Skipping already declared method Windows.UI.Popups.PopupMenu.PopupMenu()
		// Forced skipping of method Windows.UI.Popups.PopupMenu.PopupMenu()
		// Forced skipping of method Windows.UI.Popups.PopupMenu.Commands.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.IAsyncOperation<global::Windows.UI.Popups.IUICommand> ShowAsync( global::Windows.Foundation.Point invocationPoint)
		{
			throw new global::System.NotImplementedException("The member IAsyncOperation<IUICommand> PopupMenu.ShowAsync(Point invocationPoint) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=IAsyncOperation%3CIUICommand%3E%20PopupMenu.ShowAsync%28Point%20invocationPoint%29");
		}
		#endif
		// Skipping already declared method Windows.UI.Popups.PopupMenu.ShowForSelectionAsync(Windows.Foundation.Rect)
		// Skipping already declared method Windows.UI.Popups.PopupMenu.ShowForSelectionAsync(Windows.Foundation.Rect, Windows.UI.Popups.Placement)
	}
}
