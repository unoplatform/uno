#if __IOS__ || __ANDROID__
using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Markup;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[ContentProperty(Name = "Content")]
	partial class NativeContainer : DependencyObject
	{
		public UIElement Content
		{
			get { return (UIElement)GetValue(ContentProperty); }
			set { SetValue(ContentProperty, value); }
		}

		// Using a DependencyProperty as the backing store for Content.  This enables animation, styling, binding, etc...
		public static readonly DependencyProperty ContentProperty =
			DependencyProperty.Register("Content", typeof(UIElement), typeof(NativeContainer), new PropertyMetadata(null, (o, e) => ((NativeContainer)o).OnContentChanged((UIElement)e.OldValue, (UIElement)e.NewValue)));
	}
}

#endif
