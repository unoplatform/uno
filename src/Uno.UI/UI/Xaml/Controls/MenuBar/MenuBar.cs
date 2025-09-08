using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;

namespace Microsoft.UI.Xaml.Controls
{
	[ContentProperty(Name = nameof(Items))]
	public partial class MenuBar : Control
	{
		private Grid m_layoutRoot;
		private ItemsControl m_contentRoot;

		public IList<MenuBarItem> Items
		{
			get => (IList<MenuBarItem>)this.GetValue(ItemsProperty);
			private set => this.SetValue(ItemsProperty, value);
		}

		public static DependencyProperty ItemsProperty { get; } =
			DependencyProperty.Register(
				"Items",
				typeof(IList<MenuBarItem>),
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
