#if XAMARIN
using System;
using System.Collections.Generic;
using System.Text;

namespace Windows.UI.Xaml.Controls
{
	public partial class BitmapIcon : IconElement
	{
		public Uri UriSource
		{
			get => (Uri)this.GetValue(UriSourceProperty);
			set => this.SetValue(UriSourceProperty, value);
		}

		public static DependencyProperty UriSourceProperty { get; } =
			DependencyProperty.Register(
				"UriSource",
				typeof(Uri),
				typeof(BitmapIcon),
				new FrameworkPropertyMetadata(default(Uri))
			);
	}
}
#endif