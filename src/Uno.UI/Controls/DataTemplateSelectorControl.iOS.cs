using System;
using Microsoft.UI.Xaml.Controls;

#if XAMARIN_IOS_UNIFIED
using UIKit;
#elif XAMARIN_IOS
using MonoTouch.UIKit;
#endif

namespace Uno.UI.Views.Controls
{
	/// <summary>
	/// Windows Phone style DataTemplateSelector for WinRT to enable the use of data bindings on the selectors.
	/// </summary>
	public abstract partial class DataTemplateSelectorControl : ContentControl
	{
		protected override void OnContentChanged(object oldContent, object newContent)
		{
			base.OnContentChanged(oldContent, newContent);
			ContentTemplate = SelectTemplateFactory(newContent);
		}

		protected abstract Func<UIView> SelectTemplateFactory(object item);
	}
}
