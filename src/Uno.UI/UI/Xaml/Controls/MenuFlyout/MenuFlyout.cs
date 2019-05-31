using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Uno.Extensions;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Markup;

namespace Windows.UI.Xaml.Controls
{
	[ContentProperty(Name = "Items")]
	public partial class MenuFlyout : FlyoutBase
    {
		private MenuFlyoutPresenter _presenter;

		public MenuFlyout()
		{
			var collection = new ObservableVector<MenuFlyoutItemBase>();
			collection.VectorChanged += OnItemsVectorChanged;

			Items = collection;
		}

		private void OnItemsVectorChanged(IObservableVector<MenuFlyoutItemBase> sender, IVectorChangedEventArgs e)
		{
			if (_presenter != null)
			{
				var index = e.Index;
				switch (e.CollectionChange)
				{
					case CollectionChange.ItemInserted:
						_presenter.Items.Insert((int)index, Items[(int)index]);
						break;
					case CollectionChange.ItemRemoved:
						_presenter.Items.RemoveAt((int)index);
						break;
					default:
						break;
				}
			}
		}

		#region Items DependencyProperty

		public IList<MenuFlyoutItemBase> Items
		{
			get { return (IList<MenuFlyoutItemBase>)this.GetValue(ItemsProperty); }
			private set { this.SetValue(ItemsProperty, value); }
		}

		public static readonly DependencyProperty ItemsProperty =
			DependencyProperty.Register(
				"Items",
				typeof(IList<MenuFlyoutItemBase>),
				typeof(MenuFlyoutItem),
				new PropertyMetadata(defaultValue: null)
			);

		#endregion

		public global::Windows.UI.Xaml.Style MenuFlyoutPresenterStyle
		{
			get => (Style)this.GetValue(MenuFlyoutPresenterStyleProperty);
			set => SetValue(MenuFlyoutPresenterStyleProperty, value);
		}

		public static global::Windows.UI.Xaml.DependencyProperty MenuFlyoutPresenterStyleProperty { get; } =
		Windows.UI.Xaml.DependencyProperty.Register(
			"MenuFlyoutPresenterStyle",
			typeof(global::Windows.UI.Xaml.Style),
			typeof(global::Windows.UI.Xaml.Controls.MenuFlyout),
			new FrameworkPropertyMetadata(null));

		public void ShowAt(global::Windows.UI.Xaml.UIElement targetElement, global::Windows.Foundation.Point point)
		{
			base.ShowAt((FrameworkElement)targetElement);
		}

		protected override Control CreatePresenter()
		{
			_presenter = new MenuFlyoutPresenter();

			_presenter.Items.AddRange(Items);

			return _presenter;
		}
	}
}
