#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Documents
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class TextPointer 
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Documents.LogicalDirection LogicalDirection
		{
			get
			{
				throw new global::System.NotImplementedException("The member LogicalDirection TextPointer.LogicalDirection is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int Offset
		{
			get
			{
				throw new global::System.NotImplementedException("The member int TextPointer.Offset is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.DependencyObject Parent
		{
			get
			{
				throw new global::System.NotImplementedException("The member DependencyObject TextPointer.Parent is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.FrameworkElement VisualParent
		{
			get
			{
				throw new global::System.NotImplementedException("The member FrameworkElement TextPointer.VisualParent is not implemented in Uno.");
			}
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Documents.TextPointer.Parent.get
		// Forced skipping of method Windows.UI.Xaml.Documents.TextPointer.VisualParent.get
		// Forced skipping of method Windows.UI.Xaml.Documents.TextPointer.LogicalDirection.get
		// Forced skipping of method Windows.UI.Xaml.Documents.TextPointer.Offset.get
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.Foundation.Rect GetCharacterRect( global::Windows.UI.Xaml.Documents.LogicalDirection direction)
		{
			throw new global::System.NotImplementedException("The member Rect TextPointer.GetCharacterRect(LogicalDirection direction) is not implemented in Uno.");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Documents.TextPointer GetPositionAtOffset( int offset,  global::Windows.UI.Xaml.Documents.LogicalDirection direction)
		{
			throw new global::System.NotImplementedException("The member TextPointer TextPointer.GetPositionAtOffset(int offset, LogicalDirection direction) is not implemented in Uno.");
		}
		#endif
	}
}
