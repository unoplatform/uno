using Android.Views;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Controls;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using Uno.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using Uno.Extensions.Specialized;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Controls.Legacy
{
	public partial class HorizontalListView : BindableHorizontalListView, IListView, DependencyObject
	{
		private readonly SerialDisposable _clickRegistration = new SerialDisposable();

		public HorizontalListView() : base(ContextHelper.Current, null)
		{
			InitializeBinder();
			SetClipToPadding(false);
			IFrameworkElementHelper.Initialize(this);
		}

		partial void OnLoadedPartial()
		{
			BindableAdapter.ItemContainerStyle = ItemContainerStyle;
			BindableAdapter.ItemClickCommand = ItemClickCommand;
			BindableAdapter.ItemContainerTemplate = () => new ListViewItem() { ShouldHandlePressed = false };
			BindableAdapter.ItemContainerHolderStretchOrientation = Orientation.Vertical;

			SetupItemClickListeners();
		}

		partial void OnUnloadedPartial()
		{
			_clickRegistration.Disposable = null;
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			var widthMode = ViewHelper.MeasureSpecGetMode(widthMeasureSpec);

			if (widthMode == MeasureSpecMode.Unspecified)
			{
				// If the list view is measured with an Unspecified width
				// the returned size is the width of the first item of the collection.
				// For the HorizontalListView to measure all its children, it needs to be measured with 
				// an AtMost mode.
				widthMeasureSpec = ViewHelper.MakeMeasureSpec(int.MaxValue, MeasureSpecMode.AtMost);
			}

			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}

		public new object ItemsSource
		{
			get { return base.ItemsSource; }
			set { base.ItemsSource = value as IEnumerable; }
		}

		public DataTemplate ItemTemplate
		{
			get { return base.BindableAdapter.ItemTemplate; }
			set { base.BindableAdapter.ItemTemplate = value; }
		}

		public DataTemplateSelector ItemTemplateSelector
		{
			get { return base.BindableAdapter.ItemTemplateSelector; }
			set { base.BindableAdapter.ItemTemplateSelector = value; }
		}

		public ItemsPanelTemplate ItemsPanel { get; set; }

		public Thickness Padding
		{
			get
			{
				return new Thickness
				{
					Left = PaddingLeft,
					Top = PaddingTop,
					Right = PaddingRight,
					Bottom = PaddingBottom,
				};
			}

			set
			{
				var physicalPixels = ViewHelper.LogicalToPhysicalPixels(value);
				this.SetPadding(
					(int)physicalPixels.Left,
					(int)physicalPixels.Top,
					(int)physicalPixels.Right,
					(int)physicalPixels.Bottom
				);
			}
		}

		#region SelectionMode Dependency Property

		public static DependencyProperty SelectionModeProperty { get; } =
			DependencyProperty.Register(
				"SelectionMode",
				typeof(ListViewSelectionMode),
				typeof(HorizontalListView),
				new FrameworkPropertyMetadata(defaultValue: ListViewSelectionMode.None, propertyChangedCallback: OnSelectionModeChanged)
			);
		public ListViewSelectionMode SelectionMode
		{
			get { return (ListViewSelectionMode)this.GetValue(SelectionModeProperty); }
			set { this.SetValue(SelectionModeProperty, value); }
		}
		#endregion

		#region SelectedItems Dependency Property

		public object[] SelectedItems
		{
			get { return (object[])this.GetValue(SelectedItemsProperty); }
			set { this.SetValue(SelectedItemsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelectedItems.  This enables animation, styling, binding, etc...
		public static DependencyProperty SelectedItemsProperty { get; } =
			DependencyProperty.Register(
				"SelectedItems",
				typeof(object[]),
				typeof(HorizontalListView),
				new FrameworkPropertyMetadata(
					defaultValue: Array.Empty<object>(),
					propertyChangedCallback: OnSelectedItemsChanged
				)
			);
		#endregion

		#region SelectedItem Dependency Property

		public static DependencyProperty SelectedItemProperty { get; } =
			DependencyProperty.Register(
				"SelectedItem",
				typeof(object),
				typeof(HorizontalListView),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					propertyChangedCallback: (s, e) => (s as HorizontalListView).OnSelectedItemChanged(e.OldValue, e.NewValue)
				)
			);

		public new object SelectedItem
		{
			get { return (object)this.GetValue(SelectedItemProperty); }
			set { this.SetValue(SelectedItemProperty, value); }
		}
		#endregion

		#region ItemContainerStyle Dependency Property

		public static DependencyProperty ItemContainerStyleProperty { get; } =
			DependencyProperty.Register(
				"ItemContainerStyle",
				typeof(Style),
				typeof(HorizontalListView),
				new FrameworkPropertyMetadata(defaultValue: null, options: FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext, propertyChangedCallback: OnItemContainerStyleChanged)
			);

		public Style ItemContainerStyle
		{
			get { return (Style)this.GetValue(ItemContainerStyleProperty); }
			set { this.SetValue(ItemContainerStyleProperty, value); }
		}

		#endregion

		#region IsItemClickEnabled Dependency Property

		public bool IsItemClickEnabled
		{
			get { return (bool)GetValue(IsItemClickEnabledProperty); }
			set { SetValue(IsItemClickEnabledProperty, value); }
		}

		public static DependencyProperty IsItemClickEnabledProperty { get; } =
			DependencyProperty.Register("IsItemClickEnabled", typeof(bool), typeof(HorizontalListView), new FrameworkPropertyMetadata(false));
		#endregion

		public event SelectionChangedEventHandler SelectionChanged;

		private static void OnItemContainerStyleChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var listView = dependencyObject as HorizontalListView;

			if (listView != null && listView.BindableAdapter != null)
			{
				listView.BindableAdapter.ItemContainerStyle = args.NewValue as Style;
			}
		}
		private static void OnSelectionModeChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var listView = dependencyObject as HorizontalListView;

			if (listView != null)
			{
				var newMode = (ListViewSelectionMode)args.NewValue;
				var oldMode = (ListViewSelectionMode)args.OldValue;

				if (newMode == ListViewSelectionMode.None)
				{
					listView.SelectedItems = Array.Empty<object>();
				}
				else if (newMode == ListViewSelectionMode.Single && oldMode == ListViewSelectionMode.Multiple)
				{
					var firstSelection = listView.SelectedItems.FirstOrDefault();

					listView.SelectedItems = firstSelection.SelectOrDefault(s => new[] { s }, Array.Empty<object>());
				}
			}
		}

		private static void OnSelectedItemsChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var listView = dependencyObject as HorizontalListView;

			if (listView != null)
			{
				listView.UpdateSelection((object[])args.OldValue, (object[])args.NewValue);
			}
		}

		private void OnSelectedItemChanged(object oldSelectedItem, object selectedItem)
		{
			UpdateSelection(new[] { oldSelectedItem }, new[] { selectedItem });
		}

		private void UpdateSelection(object[] oldSelection, object[] newSelection)
		{
			if (BindableAdapter != null)
			{
				if (!BindableAdapter.SelectedItems.SequenceEqual(newSelection))
				{
					var removed = BindableAdapter
							.SelectedItems
							.Except(newSelection)
							.ToArray();

					var added = newSelection
					.Except(BindableAdapter.SelectedItems)
						.ToArray();

					removed.ForEach((object i) => BindableAdapter.SetItemSelection(i, null, false));
					added.ForEach((object i) => BindableAdapter.SetItemSelection(i, null, true));
				}
			}

			SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(this, oldSelection, newSelection));
		}

		protected override void SetupItemClickListeners()
		{
			_clickRegistration.Disposable = Disposable.Create(() => base.ItemClick -= OnItemClick);

			base.ItemClick += OnItemClick;
		}

		private void OnItemClick(object sender, ItemClickEventArgs args)
		{
			if (IsItemClickEnabled)
			{
				ExecuteCommandOnItem(ItemClickCommand, args.Position);
			}

			HandleItemSelection(args);
		}

		private void HandleItemSelection(ItemClickEventArgs args)
		{
			if (SelectionMode != ListViewSelectionMode.None)
			{
				var newSelection = (ItemsSource as IEnumerable).ElementAt(args.Position);

				switch (SelectionMode)
				{
					case ListViewSelectionMode.Single:
						var selectedItem = BindableAdapter.SelectedItems.FirstOrDefault();

						// Unselect the current item only if a new selection is made or 
						// the option to unselect the current item is activated.
						if (selectedItem != null && (selectedItem != newSelection))
						{
							BindableAdapter.SetItemSelection(selectedItem, null, false);
						}

						if (selectedItem != newSelection)
						{
							BindableAdapter.SetItemSelection(
								newSelection,
								args.View as SelectorItem,
								true
							);
						}

						SelectedItem = newSelection;
						break;

					case ListViewSelectionMode.Multiple:
						BindableAdapter.SetItemSelection(
							(ItemsSource as IEnumerable).ElementAt(args.Position),
							args.View as SelectorItem,
							!BindableAdapter.SelectedItems.Contains(newSelection)
						);
						break;
				}

				SelectedItems = BindableAdapter.SelectedItems.ToArray();

			}
		}

		/// <summary>
		/// Scrolls the list to bring the specified data item into view.
		/// </summary>
		/// <param name="item">The data item to bring into view.</param>
		public void ScrollIntoView(object item)
		{
			var index = IndexForItem(item);
			if (index != -1)
			{
				ScrollIntoViewInner(index);
			}
		}

		private int IndexForItem(object item)
		{
			return (ItemsSource as IEnumerable)?.IndexOf(item) ?? -1;
		}
	}
}
