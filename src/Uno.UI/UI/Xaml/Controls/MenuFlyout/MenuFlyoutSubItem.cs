#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Items")]
	public  partial class MenuFlyoutSubItem : MenuFlyoutItemBase
	{
		public MenuFlyoutSubItem()
		{
			Items = new ObservableCollection<MenuFlyoutItemBase>();

			DefaultStyleKey = typeof(MenuFlyoutSubItem);
		}

		public  string Text
		{
			get => (string)this.GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		public  global::System.Collections.Generic.IList<MenuFlyoutItemBase> Items
		{
			get;
		}

		public  IconElement Icon
		{
			get => (IconElement)this.GetValue(IconProperty);
			set => this.SetValue(IconProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty TextProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Text",
			typeof(string), 
			typeof(MenuFlyoutSubItem), 
			new FrameworkPropertyMetadata(default(string)));

		public static global::Windows.UI.Xaml.DependencyProperty IconProperty { get; } = 
		Windows.UI.Xaml.DependencyProperty.Register(
			"Icon",
			typeof(IconElement), 
			typeof(MenuFlyoutSubItem), 
			new FrameworkPropertyMetadata(default(IconElement)));
	}
}
