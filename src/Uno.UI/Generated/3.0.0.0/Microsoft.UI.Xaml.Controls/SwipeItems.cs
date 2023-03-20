#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SwipeItems : global::Windows.UI.Xaml.DependencyObject,global::System.Collections.Generic.IList<global::Microsoft.UI.Xaml.Controls.SwipeItem>,global::System.Collections.Generic.IEnumerable<global::Microsoft.UI.Xaml.Controls.SwipeItem>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Microsoft.UI.Xaml.Controls.SwipeMode Mode
		{
			get
			{
				return (global::Microsoft.UI.Xaml.Controls.SwipeMode)this.GetValue(ModeProperty);
			}
			set
			{
				this.SetValue(ModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint SwipeItems.Size is not implemented. For more information, visit https://aka.platform.uno/notimplemented?m=uint%20SwipeItems.Size");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Mode), typeof(global::Microsoft.UI.Xaml.Controls.SwipeMode), 
			typeof(global::Microsoft.UI.Xaml.Controls.SwipeItems), 
			new FrameworkPropertyMetadata(default(global::Microsoft.UI.Xaml.Controls.SwipeMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public SwipeItems() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Microsoft.UI.Xaml.Controls.SwipeItems", "SwipeItems.SwipeItems()");
		}
		#endif
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.SwipeItems()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.Mode.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.Mode.set
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.GetAt(uint)
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.Size.get
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.GetView()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.IndexOf(Microsoft.UI.Xaml.Controls.SwipeItem, out uint)
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.SetAt(uint, Microsoft.UI.Xaml.Controls.SwipeItem)
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.InsertAt(uint, Microsoft.UI.Xaml.Controls.SwipeItem)
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.RemoveAt(uint)
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.Append(Microsoft.UI.Xaml.Controls.SwipeItem)
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.RemoveAtEnd()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.Clear()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.GetMany(uint, Microsoft.UI.Xaml.Controls.SwipeItem[])
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.ReplaceAll(Microsoft.UI.Xaml.Controls.SwipeItem[])
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.First()
		// Forced skipping of method Microsoft.UI.Xaml.Controls.SwipeItems.ModeProperty.get
		// Processing: System.Collections.Generic.IList<Microsoft.UI.Xaml.Controls.SwipeItem>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IList<Microsoft.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public int IndexOf( global::Microsoft.UI.Xaml.Controls.SwipeItem item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IList<Microsoft.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void Insert( int index,  global::Microsoft.UI.Xaml.Controls.SwipeItem item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IList<Microsoft.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void RemoveAt( int index)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Microsoft.UI.Xaml.Controls.SwipeItem this[int index]
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		// Processing: System.Collections.Generic.ICollection<Microsoft.UI.Xaml.Controls.SwipeItem>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<Microsoft.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void Add( global::Microsoft.UI.Xaml.Controls.SwipeItem item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<Microsoft.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void Clear()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<Microsoft.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool Contains( global::Microsoft.UI.Xaml.Controls.SwipeItem item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<Microsoft.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void CopyTo( global::Microsoft.UI.Xaml.Controls.SwipeItem[] array,  int arrayIndex)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<Microsoft.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool Remove( global::Microsoft.UI.Xaml.Controls.SwipeItem item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public int Count
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool IsReadOnly
		{
			get
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		// Processing: System.Collections.Generic.IEnumerable<Microsoft.UI.Xaml.Controls.SwipeItem>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IEnumerable<Microsoft.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::System.Collections.Generic.IEnumerator<global::Microsoft.UI.Xaml.Controls.SwipeItem> GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		// Processing: System.Collections.IEnumerable
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.IEnumerable
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		 global::System.Collections.IEnumerator global::System.Collections.IEnumerable.GetEnumerator()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
	}
}
