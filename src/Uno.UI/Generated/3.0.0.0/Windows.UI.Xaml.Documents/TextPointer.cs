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
				throw new global::System.NotImplementedException("The member LogicalDirection TextPointer.LogicalDirection is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=LogicalDirection%20TextPointer.LogicalDirection");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  int Offset
		{
			get
			{
				throw new global::System.NotImplementedException("The member int TextPointer.Offset is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=int%20TextPointer.Offset");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.DependencyObject Parent
		{
			get
			{
				throw new global::System.NotImplementedException("The member DependencyObject TextPointer.Parent is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=DependencyObject%20TextPointer.Parent");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.FrameworkElement VisualParent
		{
			get
			{
				throw new global::System.NotImplementedException("The member FrameworkElement TextPointer.VisualParent is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=FrameworkElement%20TextPointer.VisualParent");
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
			throw new global::System.NotImplementedException("The member Rect TextPointer.GetCharacterRect(LogicalDirection direction) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=Rect%20TextPointer.GetCharacterRect%28LogicalDirection%20direction%29");
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Documents.TextPointer GetPositionAtOffset( int offset,  global::Windows.UI.Xaml.Documents.LogicalDirection direction)
		{
			throw new global::System.NotImplementedException("The member TextPointer TextPointer.GetPositionAtOffset(int offset, LogicalDirection direction) is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=TextPointer%20TextPointer.GetPositionAtOffset%28int%20offset%2C%20LogicalDirection%20direction%29");
		}
		#endif
	}
}