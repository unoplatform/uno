#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name="Items")]
	public partial class MenuBar : Control
	{
		private Grid m_layoutRoot;
		private ItemsControl m_contentRoot;

		public IList<MenuBarItem> Items
		{
			get => (IList<MenuBarItem>)this.GetValue(ItemsProperty);
			private set => this.SetValue(ItemsProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty ItemsProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"Items",
			typeof(global::System.Collections.Generic.IList<MenuBarItem>),
			typeof(MenuBar),
			new FrameworkPropertyMetadata(null)
		);

		public MenuBar() : base()
		{
			Items = new ObservableCollection<MenuBarItem>();

			DefaultStyleKey = typeof(MenuBar);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			m_layoutRoot = GetTemplateChild("LayoutRoot") as Grid;

			if (GetTemplateChild("ContentRoot") is ItemsControl contentRoot)
			{
				contentRoot.XYFocusKeyboardNavigation = XYFocusKeyboardNavigationMode.Enabled;

				contentRoot.ItemsSource = Items;

				m_contentRoot = contentRoot;
			}
		}

		internal bool IsFlyoutOpen { get; set; }
	}
}
