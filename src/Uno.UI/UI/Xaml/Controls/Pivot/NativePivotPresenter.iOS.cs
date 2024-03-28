using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Linq;
using UIKit;
using Uno.Extensions;

namespace Windows.UI.Xaml.Controls
{
	public partial class NativePivotPresenter
	{
		private Panel _content;
		private UITabBar _tabBar;

		partial void InitializePartial()
		{
			//This is present in order to be able to stretch the content and tabbar
			this.HorizontalContentAlignment = HorizontalAlignment.Stretch;
			this.VerticalContentAlignment = VerticalAlignment.Stretch;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_content = this.GetTemplateChild("PART_Content") as Panel;
			_tabBar = this.FindFirstChild<UITabBar>();

			if (_content == null)
			{
				throw new InvalidOperationException($"PART_Content is missing");
			}

			if (_tabBar == null)
			{
				throw new InvalidOperationException("Unable to find a UITabBar in the control template");
			}
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			UpdateItems();

			_tabBar.ItemSelected += OnItemSelected;
		}

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			_tabBar.ItemSelected -= OnItemSelected;
		}

		partial void UpdateItems()
		{
			if (_tabBar != null)
			{
				SynchronizeDataContext();

				var newTabs = Items
					.OfType<PivotItem>()
				   .Select(t => new UITabBarItem(t.Header?.ToString() ?? "", t.Image, t.GetHashCode()))
				   .ToArray();

				_tabBar.SetItems(newTabs, true);

				_tabBar.SelectedItem = _tabBar.Items.FirstOrDefault();

				if (_tabBar.SelectedItem != null)
				{
					SetSelectedTab((int)_tabBar.SelectedItem.Tag);
				}
			}
		}

		internal protected override void OnDataContextChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnDataContextChanged(e);

			SynchronizeDataContext();
		}

		private void SynchronizeDataContext()
			// Force set the items' local value DataContext to avoid getting inherited DataContext
			=> Items?.OfType<PivotItem>().ToList().ForEach(i => i.SetValue(PivotItem.DataContextProperty, DataContext, DependencyPropertyValuePrecedences.Local));

		private void OnItemSelected(object sender, UITabBarItemEventArgs e)
		{
			SetSelectedTab((int)e.Item.Tag);
		}

		private void SetSelectedTab(int tag)
		{
			if (_content != null)
			{
				foreach (var item in Items.OfType<PivotItem>())
				{
					if (item.GetHashCode() == tag)
					{
						if (!_content.Subviews.Contains(item))
						{
							_content.Add(item);
						}

						item.Visibility = Visibility.Visible;
					}
					else
					{
						item.Visibility = Visibility.Collapsed;
					}
				}
			}
		}
	}
}
