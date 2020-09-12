#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
	[global::Uno.NotImplemented]
	#endif
	public  partial class SwipeItems : global::Windows.UI.Xaml.DependencyObject,global::System.Collections.Generic.IList<global::Windows.UI.Xaml.Controls.SwipeItem>,global::System.Collections.Generic.IEnumerable<global::Windows.UI.Xaml.Controls.SwipeItem>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint SwipeItems.Size is not implemented in Uno.");
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  global::Windows.UI.Xaml.Controls.SwipeMode Mode
		{
			get
			{
				return (global::Windows.UI.Xaml.Controls.SwipeMode)this.GetValue(ModeProperty);
			}
			set
			{
				this.SetValue(ModeProperty, value);
			}
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public static global::Windows.UI.Xaml.DependencyProperty ModeProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			nameof(Mode), typeof(global::Windows.UI.Xaml.Controls.SwipeMode), 
			typeof(global::Windows.UI.Xaml.Controls.SwipeItems), 
			new FrameworkPropertyMetadata(default(global::Windows.UI.Xaml.Controls.SwipeMode)));
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || false || false || false || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__MACOS__")]
		public SwipeItems() : base()
		{
			global::Windows.Foundation.Metadata.ApiInformation.TryRaiseNotImplemented("Windows.UI.Xaml.Controls.SwipeItems", "SwipeItems.SwipeItems()");
		}
		#endif
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.SwipeItems()
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.Mode.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.Mode.set
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.GetAt(uint)
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.Size.get
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.GetView()
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.IndexOf(Windows.UI.Xaml.Controls.SwipeItem, out uint)
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.SetAt(uint, Windows.UI.Xaml.Controls.SwipeItem)
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.InsertAt(uint, Windows.UI.Xaml.Controls.SwipeItem)
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.RemoveAt(uint)
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.Append(Windows.UI.Xaml.Controls.SwipeItem)
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.RemoveAtEnd()
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.Clear()
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.GetMany(uint, Windows.UI.Xaml.Controls.SwipeItem[])
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.ReplaceAll(Windows.UI.Xaml.Controls.SwipeItem[])
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.First()
		// Forced skipping of method Windows.UI.Xaml.Controls.SwipeItems.ModeProperty.get
		// Processing: System.Collections.Generic.IList<Windows.UI.Xaml.Controls.SwipeItem>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IList<Windows.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public int IndexOf( global::Windows.UI.Xaml.Controls.SwipeItem item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IList<Windows.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void Insert( int index,  global::Windows.UI.Xaml.Controls.SwipeItem item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IList<Windows.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void RemoveAt( int index)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::Windows.UI.Xaml.Controls.SwipeItem this[int index]
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
		// Processing: System.Collections.Generic.ICollection<Windows.UI.Xaml.Controls.SwipeItem>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<Windows.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void Add( global::Windows.UI.Xaml.Controls.SwipeItem item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<Windows.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void Clear()
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<Windows.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool Contains( global::Windows.UI.Xaml.Controls.SwipeItem item)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<Windows.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public void CopyTo( global::Windows.UI.Xaml.Controls.SwipeItem[] array,  int arrayIndex)
		{
			throw new global::System.NotSupportedException();
		}
		#endif
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.ICollection<Windows.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public bool Remove( global::Windows.UI.Xaml.Controls.SwipeItem item)
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
			set
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
			set
			{
				throw new global::System.NotSupportedException();
			}
		}
		#endif
		// Processing: System.Collections.Generic.IEnumerable<Windows.UI.Xaml.Controls.SwipeItem>
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		// DeclaringType: System.Collections.Generic.IEnumerable<Windows.UI.Xaml.Controls.SwipeItem>
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public global::System.Collections.Generic.IEnumerator<global::Windows.UI.Xaml.Controls.SwipeItem> GetEnumerator()
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
