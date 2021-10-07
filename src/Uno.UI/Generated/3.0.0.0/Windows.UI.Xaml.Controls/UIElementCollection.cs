#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Windows.UI.Xaml.Controls
{
	#if false || false || false || false || false || false || false
	[global::Uno.NotImplemented]
	#endif
	public  partial class UIElementCollection : global::System.Collections.Generic.IList<global::Windows.UI.Xaml.UIElement>,global::System.Collections.Generic.IEnumerable<global::Windows.UI.Xaml.UIElement>
	{
		#if __ANDROID__ || __IOS__ || NET461 || __WASM__ || __SKIA__ || __NETSTD_REFERENCE__ || __MACOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__WASM__", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
		public  uint Size
		{
			get
			{
				throw new global::System.NotImplementedException("The member uint UIElementCollection.Size is not implemented in Uno.");
			}
		}
		#endif
		// Skipping already declared method Windows.UI.Xaml.Controls.UIElementCollection.Move(uint, uint)
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.GetAt(uint)
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.Size.get
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.GetView()
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.IndexOf(Windows.UI.Xaml.UIElement, out uint)
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.SetAt(uint, Windows.UI.Xaml.UIElement)
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.InsertAt(uint, Windows.UI.Xaml.UIElement)
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.RemoveAt(uint)
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.Append(Windows.UI.Xaml.UIElement)
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.RemoveAtEnd()
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.Clear()
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.GetMany(uint, Windows.UI.Xaml.UIElement[])
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.ReplaceAll(Windows.UI.Xaml.UIElement[])
		// Forced skipping of method Windows.UI.Xaml.Controls.UIElementCollection.First()
		// Processing: System.Collections.Generic.IList<Windows.UI.Xaml.UIElement>
		// Skipping already implement System.Collections.Generic.IList<Windows.UI.Xaml.UIElement>.this[int]
		// Processing: System.Collections.Generic.ICollection<Windows.UI.Xaml.UIElement>
		// Skipping already implement System.Collections.Generic.ICollection<Windows.UI.Xaml.UIElement>.Count
		// Skipping already implement System.Collections.Generic.ICollection<Windows.UI.Xaml.UIElement>.IsReadOnly
		// Processing: System.Collections.Generic.IEnumerable<Windows.UI.Xaml.UIElement>
		// Processing: System.Collections.IEnumerable
	}
}
