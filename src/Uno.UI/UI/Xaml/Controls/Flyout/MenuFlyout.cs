using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Items")]
	public partial class MenuFlyout : FlyoutBase
    {
		public MenuFlyout()
		{
			Items = new DependencyObjectCollection<MenuFlyoutItem>(this);
		}

		#region Items DependencyProperty

		public IList<MenuFlyoutItem> Items
		{
			get { return (IList<MenuFlyoutItem>)this.GetValue(ItemsProperty); }
			private set { this.SetValue(ItemsProperty, value); }
		}

		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register(
				"Items",
				typeof(IList<MenuFlyoutItem>),
				typeof(MenuFlyout),
				new PropertyMetadata(defaultValue: null)
			);

		#endregion
	}
}