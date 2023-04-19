using System.Collections;

#if XAMARIN_ANDROID
using View = Windows.UI.Xaml.UIElement;
#elif XAMARIN_IOS_UNIFIED
using View = UIKit.UIView;
#elif XAMARIN_IOS
using View = MonoTouch.UIKit.UIView;
#elif __MACOS__
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls;

partial class Panel : IEnumerable
{
	/// <summary>        
	/// Support for the C# collection initializer style.
	/// Allows items to be added like this 
	/// new Panel 
	/// {
	///    new Border()
	/// }
	/// </summary>
	/// <param name="view"></param>
	public
#if __IOS__
		new
#endif
		void Add(View view)
	{
		Children.Add(view);
	}

#if !__IOS__ // UIView already implements IEnumerable
	public IEnumerator GetEnumerator() => this.GetChildren().GetEnumerator();
#endif
}
