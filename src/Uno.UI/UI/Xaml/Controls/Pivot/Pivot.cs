using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.Extensions;
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
		private bool _isTemplateApplied;

		private bool _isUWPTemplate;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			_staticHeader = this.GetTemplateChild("StaticHeader") as PivotHeaderPanel;
			_titleContentControl = this.GetTemplateChild("TitleContentControl") as ContentControl;
			_header = this.GetTemplateChild("Header") as PivotHeaderPanel;
			_headerClipperGeometry = this.GetTemplateChild("HeaderClipperGeometry") as RectangleGeometry;
			_headerClipper = this.GetTemplateChild("HeaderClipper") as ContentControl;

			_isUWPTemplate = _staticHeader != null;

			if (!_isUWPTemplate)
			{
				ItemsPanelRoot = null;
			}

			_isTemplateApplied = true;

			Loaded += (s, e) => RegisterHeaderEvents();
			Unloaded += (s, e) => UnregisterHeaderEvents();
			Items.VectorChanged += OnItemsVectorChanged;

			UpdateProperties();
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

				_header.Visibility = Visibility.Collapsed;
				_titleContentControl.Visibility = Title != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}

		protected override bool IsItemItsOwnContainerOverride(object item)
			=> _isUWPTemplate ? item is PivotItem : base.IsItemItsOwnContainerOverride(item);

		protected override DependencyObject GetContainerForItemOverride()
			=> _isUWPTemplate ? new PivotItem() : GetContainerForItemOverride();

		private void OnItemsVectorChanged(IObservableVector<object> sender, IVectorChangedEventArgs @event)
		{
			SynchronizeItems();
		}

		private void SynchronizeItems()
		{
			if (_isUWPTemplate)
			{
				_staticHeader.Visibility = Items.Count != 0 ? Visibility.Visible : Visibility.Collapsed;

				UnregisterHeaderEvents();
				_staticHeader.Children.Clear();

				foreach (var item in Items)
				{
					if (item is PivotItem pivotItem)
					{
						var headerItem = new PivotHeaderItem()
						{
							Content = pivotItem.Header,
							ContentTemplate = HeaderTemplate,
							IsHitTestVisible = true
						};

						headerItem.SetBinding(
							ContentControl.ContentTemplateProperty,
							new Binding
							{
								Path = "HeaderTemplate",
								RelativeSource = RelativeSource.TemplatedParent
							}
						);

						_staticHeader.Children.Add(headerItem);
					}
				}

				if (SelectedIndex == -1 && Items.Count != 0)
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

		private void SynchronizeSelectedItem()
		{
			if (SelectedIndex != -1 && Items.Count != 0)
			{
				var selectedIndex = Math.Max(Math.Min(SelectedIndex, Items.Count - 1), 0);

				var selectedHeader = _staticHeader.Children[selectedIndex] as PivotHeaderItem;
				var selectedPivotitem = Items[selectedIndex] as PivotItem;

				SelectedItem = selectedPivotitem;

				foreach (var item in Items)
				{
					if (item is PivotItem pivotItem && item != selectedPivotitem)
					{
						pivotItem.Visibility = Visibility.Collapsed;
					}
				}

				selectedPivotitem.Visibility = Visibility.Visible;

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
	}
}
