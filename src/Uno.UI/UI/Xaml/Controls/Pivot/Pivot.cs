using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.Extensions;
using Uno.Extensions.Specialized;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Documents;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Windows.UI.Xaml.Controls
{
	public partial class Pivot : ItemsControl
	{
		private ContentControl _titleContentControl;
		private PivotHeaderPanel _staticHeader;
		private PivotHeaderPanel _header;
		private RectangleGeometry _headerClipperGeometry;
		private ContentControl _headerClipper;
		private DataTemplate _pivotItemTemplate;
		private bool _isTemplateApplied;

		private bool _isUWPTemplate;

		public Pivot()
		{
			DefaultStyleKey = typeof(Pivot);
			Loaded += (s, e) => RegisterHeaderEvents();
			Unloaded += (s, e) => UnregisterHeaderEvents();
			Items.VectorChanged += OnItemsVectorChanged;
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_staticHeader = this.GetTemplateChild("StaticHeader") as PivotHeaderPanel;
			_titleContentControl = this.GetTemplateChild("TitleContentControl") as ContentControl;
			_header = this.GetTemplateChild("Header") as PivotHeaderPanel;
			_headerClipperGeometry = this.GetTemplateChild("HeaderClipperGeometry") as RectangleGeometry;
			_headerClipper = this.GetTemplateChild("HeaderClipper") as ContentControl;
			_pivotItemTemplate = new DataTemplate(() => new PivotItem());

			_isUWPTemplate = _staticHeader != null;

			if (!_isUWPTemplate)
			{
				ItemsPanelRoot = null;
			}

			_isTemplateApplied = true;

			UpdateProperties();

#if __WASM__ || __SKIA__
			//TODO: Workaround for https://github.com/unoplatform/uno/issues/5144
			//OnApplyTemplate() is comming too late when using bindings
			SetNeedsUpdateItems();
#endif

			SynchronizeItems();
		}

		private void UnregisterHeaderEvents()
		{
			if (_staticHeader != null)
			{
				foreach (var item in _staticHeader.Children)
				{
					if (item is PivotHeaderItem pivotHeaderItem)
					{
						pivotHeaderItem.PointerPressed -= OnItemPointerPressed;
					}
				}
			}
		}

		private void RegisterHeaderEvents()
		{
			UnregisterHeaderEvents();

			if (_staticHeader != null)
			{
				foreach (var item in _staticHeader.Children)
				{
					if (item is PivotHeaderItem pivotHeaderItem)
					{
						pivotHeaderItem.PointerPressed += OnItemPointerPressed;
					}
				}
			}
		}

		private void UpdateProperties()
		{
			if (
				_isUWPTemplate
				&& _isTemplateApplied
			)
			{
				if(_headerClipper != null)
				{
					// Disable clipping until it gets properly supported.
					_headerClipper.Clip = null;
				}

				if (_header != null)
				{
					_header.Visibility = Visibility.Collapsed;
				}

				if (_titleContentControl != null)
				{
					_titleContentControl.Visibility = Title != null ? Visibility.Visible : Visibility.Collapsed;
				}
			}
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
			=> _isUWPTemplate ? item is PivotItem : base.IsItemItsOwnContainerOverride(item);

		protected override DependencyObject GetContainerForItemOverride()
			=> _isUWPTemplate ? (DependencyObject)_pivotItemTemplate.LoadContentCached() : base.GetContainerForItemOverride();

		private void OnItemsVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
		{
			SynchronizeItems();
		}

		private void SynchronizeItems()
		{
			if (_isUWPTemplate)
			{
				_staticHeader.Visibility = HasItems ? Visibility.Visible : Visibility.Collapsed;

				UnregisterHeaderEvents();
				_staticHeader.Children.Clear();

				var items = GetItems();
				foreach (var item in items)
				{
					var headerItem = new PivotHeaderItem()
					{
						ContentTemplate = HeaderTemplate,
						IsHitTestVisible = true
					};

					if (item is PivotItem pivotItem)
					{
						headerItem.Content = pivotItem.Header;
					}
					else
					{
						headerItem.Content = item;
					}

					headerItem.SetBinding(
						ContentControl.ContentTemplateProperty,
						new Binding
						{
							Path = "HeaderTemplate",
							RelativeSource = RelativeSource.TemplatedParent
						}
					);

					// Materialize template to ensure visual states are set correctly.
					headerItem.EnsureTemplate();

					_staticHeader.Children.Add(headerItem);
				}

				if (SelectedIndex == -1 && HasItems)
				{
					SelectedIndex = 0;
				}
				else
				{
					SynchronizeSelectedItem();
				}

				RegisterHeaderEvents();
			}
			else
			{
				if(TemplatedRoot is NativePivotPresenter presenter)
				{
					presenter.Items.Clear();
					presenter.Items.AddRange(Items);
				}
			}
		}

		private void OnItemPointerPressed(object sender, PointerRoutedEventArgs e)
		{
			if (_isUWPTemplate)
			{
				if (sender is PivotHeaderItem selectedHeaderItem)
				{
					SelectedIndex = _staticHeader.Children.IndexOf(selectedHeaderItem);
				}
			}
		}

		private void OnSelectedIndexChanged(int oldValue, int newValue)
		{
			if (_isUWPTemplate)
			{
				SynchronizeSelectedItem();
			}
		}

		protected override void OnItemsSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnItemsSourceChanged(e);

			SynchronizeItems();
		}		

		private void SynchronizeSelectedItem()
		{
			if (SelectedIndex != -1 && HasItems)
			{
				var selectedIndex = Math.Max(Math.Min(SelectedIndex, NumberOfItems - 1), 0);

				var selectedHeader = _staticHeader.Children[selectedIndex] as PivotHeaderItem;
				var items = GetItems();
				var selectedPivotitem = items.ElementAt(selectedIndex);

				SelectedItem = selectedPivotitem;
				
				for (int i = 0; i < NumberOfItems; i++)
				{
					if (ContainerFromIndex(i) is ContentControl itemContainer)
					{
						if (selectedIndex != i)
						{
							itemContainer.Visibility = Visibility.Collapsed;
						}
					}
				}

				if (ContainerFromIndex(selectedIndex) is ContentControl pi)
				{
					pi.Visibility = Visibility.Visible;
				}

				foreach (var child in _staticHeader.Children)
				{
					if (child is PivotHeaderItem headerItem && child != selectedHeader)
					{
						VisualStateManager.GoToState(headerItem, "Unselected", true);
					}
				}

				VisualStateManager.GoToState(selectedHeader, "Selected", true);
			}
		}

		private static void OnTitlePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			if (dependencyObject is Pivot pivot)
			{
				if (pivot._isUWPTemplate)
				{
					pivot.UpdateProperties();
				}
			}
		}

		private void OnSelectedItemPropertyChanged(object oldValue, object newValue)
		{
			var removedItems = oldValue == null ? new object[0] : new [] { oldValue };
			var addedItems = newValue == null ? new object[0] : new [] { newValue };

			OnSelectedItemChangedPartial(oldValue, newValue);

			InvokeSelectionChanged(removedItems, addedItems);			
		}

		partial void OnSelectedItemChangedPartial(object oldSelectedItem, object selectedItem);

		protected void InvokeSelectionChanged(object[] removedItems, object[] addedItems)
		{
			SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(this, removedItems, addedItems));
		}
	}
}
