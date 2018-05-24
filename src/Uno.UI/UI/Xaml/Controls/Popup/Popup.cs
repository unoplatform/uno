using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Child")]
	public partial class Popup : PopupBase
	{
		internal PopupPanel PopupPanel
		{
			get { return (PopupPanel)GetValue(PopupPanelProperty); }
			set { SetValue(PopupPanelProperty, value); }
		}

		public static readonly DependencyProperty PopupPanelProperty =
			DependencyProperty.Register("PopupPanel", typeof(PopupPanel), typeof(Popup), new PropertyMetadata(null, (s, e) => ((Popup)s)?.OnPopupPanelChanged(e)));

		partial void OnPopupPanelChanged(DependencyPropertyChangedEventArgs e);
	}
}
