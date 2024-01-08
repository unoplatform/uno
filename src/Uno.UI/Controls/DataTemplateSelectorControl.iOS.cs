using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

using UIKit;

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
			ContentTemplate = new DataTemplate(SelectTemplateFactory(newContent));
		}

		protected abstract Func<UIView> SelectTemplateFactory(object item);
	}
}
