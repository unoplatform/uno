using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Child")]
	public partial class Popup : PopupBase
	{
		/// <summary>
		/// Overrides the default location of this popup (cf. Remarks)
		/// </summary>
		/// <remarks>
		/// When a Popup is opened, the <see cref="PopupPanel"/> will top/left aligned the <see cref="Child"/> to the
		/// current location of the given popup in the visual tree.
		/// However when an Anchor is set on a popup, the Child will instead be top/left aligned to the location of this Anchor.
		/// </remarks>
		internal UIElement Anchor { get; set; }

		/// <summary>
		/// The <see cref="PopupPanel"/> which host this Popup
		/// </summary>
		internal PopupPanel PopupPanel
		{
			get => (PopupPanel)GetValue(PopupPanelProperty);
			set => SetValue(PopupPanelProperty, value);
		}

		public static readonly DependencyProperty PopupPanelProperty =
			DependencyProperty.Register("PopupPanel", typeof(PopupPanel), typeof(Popup), new PropertyMetadata(null, (s, e) => ((Popup)s)?.OnPopupPanelChanged(e)));

		partial void OnPopupPanelChanged(DependencyPropertyChangedEventArgs e);
	}
}
