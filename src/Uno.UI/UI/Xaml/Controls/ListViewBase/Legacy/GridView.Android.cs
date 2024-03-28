using Android.Views;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.DataBinding;
using Uno.UI.Controls;
using Windows.UI.Xaml.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Uno.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Uno.UI.Controls.Legacy
{
	public partial class GridView : BindableGridView, IFrameworkElement, DependencyObject
	{
		private SerialDisposable _clickRegistration = new SerialDisposable();

		public GridView() : base(ContextHelper.Current)
		{
			IFrameworkElementHelper.Initialize(this);

			StretchMode = Android.Widget.StretchMode.StretchColumnWidth;
			BindableAdapter.ItemContainerTemplate = () => new GridViewItem() { ShouldHandlePressed = false };
			BindableAdapter.ItemContainerHolderStretchOrientation = Orientation.Horizontal;
			BindableAdapter.ItemContainerStyle = ItemContainerStyle;

			SetClipToPadding(false);
			ScrollBarStyle = ScrollbarStyles.OutsideOverlay; // prevents padding from affecting scrollbar position
		}

		public new event ItemClickEventHandler ItemClick;

		public ICommand Command
		{
			get { return ItemClickCommand; }
			set { ItemClickCommand = value; }
		}

		// Helper property that's not part of the Windows API
		// Is used by XXXXX as an alternative to ItemsWrapGridAdaptiveItemWitdhBehavior.ItemMaxWidth
		// Will be removed when features required by ItemsWrapGridAdaptiveItemWidthBehavior are implemented
		public int ItemMaxWidth { get; set; }

		private int GetActualItemMaxWidth(Size gridViewSize)
		{
			if (ItemMaxWidth > 0)
			{
				return ViewHelper.LogicalToPhysicalPixels(ItemMaxWidth);
			}
			else if (ChildCount > 0)
			{
				// column size is determinated by the width of the first child
				// this assumes that all children have the same width
				var firstChild = GetChildAt(0);
				firstChild.Measure(gridViewSize);
				return firstChild.MeasuredWidth;
			}
			else
			{
				return gridViewSize.Width;
			}
		}

		partial void OnLayoutPartial(bool changed, int left, int top, int right, int bottom)
		{
			LayoutChildren();
		}

		#region Padding DependencyProperty

		public Thickness Padding
		{
			get { return (Thickness)GetValue(PaddingProperty); }
			set { SetValue(PaddingProperty, value); }
		}

		public static DependencyProperty PaddingProperty { get; } =
			DependencyProperty.Register(
				"Padding",
				typeof(Thickness),
				typeof(GridView),
				new FrameworkPropertyMetadata(
					(Thickness)Thickness.Empty,
					FrameworkPropertyMetadataOptions.AffectsMeasure,
					(s, e) => ((GridView)s)?.OnPaddingChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
				)
			);

		private void OnPaddingChanged(Thickness oldValue, Thickness newValue)
		{
			var paddingInPixel = ViewHelper.LogicalToPhysicalPixels(newValue);

			this.SetPadding(
				(int)paddingInPixel.Left,
				(int)paddingInPixel.Top,
				(int)paddingInPixel.Right,
				(int)paddingInPixel.Bottom
			);
		}

		#endregion

		#region SelectionMode Dependency Property

		public static DependencyProperty SelectionModeProperty { get; } =
			DependencyProperty.Register(
				"SelectionMode",
				typeof(ListViewSelectionMode),
				typeof(GridView),
				new FrameworkPropertyMetadata(defaultValue: ListViewSelectionMode.None, propertyChangedCallback: OnSelectionModeChanged)
			);
		public ListViewSelectionMode SelectionMode
		{
			get { return (ListViewSelectionMode)this.GetValue(SelectionModeProperty); }
			set { this.SetValue(SelectionModeProperty, value); }
		}
		#endregion

		#region SelectedItems Dependency Property

		public IList<object> SelectedItems
		{
			get { return (IList<object>)this.GetValue(SelectedItemsProperty); }
			set { this.SetValue(SelectedItemsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for SelectedItems.  This enables animation, styling, binding, etc...
		public static DependencyProperty SelectedItemsProperty { get; } =
			DependencyProperty.Register(
				"SelectedItems",
				typeof(IList<object>),
				typeof(GridView),
				new FrameworkPropertyMetadata(
					defaultValue: new List<object>(),
					propertyChangedCallback: OnSelectedItemsChanged
				)
			);
		#endregion

		#region SelectedItem DependencyProperty

		public new object SelectedItem
		{
			get { return (object)GetValue(SelectedItemProperty); }
			set { SetValue(SelectedItemProperty, value); }
		}

		public static DependencyProperty SelectedItemProperty { get; } =
			DependencyProperty.Register(
				"SelectedItem",
				typeof(object),
				typeof(GridView),
				new FrameworkPropertyMetadata(
					null,
					(s, e) => ((GridView)s)?.OnSelectedItemChanged(e.OldValue, e.NewValue)));

		#endregion

		#region ItemContainerStyle DependencyProperty

		public Style ItemContainerStyle
		{
			get { return (Style)GetValue(ItemContainerStyleProperty); }
			set { SetValue(ItemContainerStyleProperty, value); }
		}

		public static DependencyProperty ItemContainerStyleProperty { get; } =
			DependencyProperty.Register(
				"ItemContainerStyle",
				typeof(Style),
				typeof(GridView),
				new FrameworkPropertyMetadata(
					(Style)null,
					FrameworkPropertyMetadataOptions.ValueDoesNotInheritDataContext,
					(s, e) => ((GridView)s)?.OnItemContainerStyleChanged((Style)e.OldValue, (Style)e.NewValue)
				)
			);

		private void OnItemContainerStyleChanged(Style oldValue, Style newValue)
		{
			BindableAdapter.ItemContainerStyle = newValue;
		}

		#endregion

		#region UnselectOnClick Dependency Property
		/// <summary>
		/// Offers the possibility to unselect the currently selected item when the SelectionMode is Single.
		/// Clicking on the currently selected item will unselect it.
		/// </summary>
		public bool UnselectOnClick
		{
			get { return (bool)GetValue(UnselectOnClickProperty); }
			set { SetValue(UnselectOnClickProperty, value); }
		}

		public static DependencyProperty UnselectOnClickProperty { get; } =
			DependencyProperty.Register("UnselectOnClick", typeof(bool), typeof(GridView), new FrameworkPropertyMetadata(default(bool)));

		#endregion

		private static void OnSelectionModeChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var gridView = dependencyObject as GridView;

			if (gridView != null)
			{
				var newMode = (ListViewSelectionMode)args.NewValue;
				var oldMode = (ListViewSelectionMode)args.OldValue;

				if (newMode == ListViewSelectionMode.None)
				{
					gridView.SelectedItems = Array.Empty<object>();
				}
				else if (newMode == ListViewSelectionMode.Single && oldMode == ListViewSelectionMode.Multiple)
				{
					var firstSelection = gridView.SelectedItems.FirstOrDefault();

					gridView.SelectedItems = firstSelection.SelectOrDefault(s => new[] { s }, Array.Empty<object>());
				}
			}
		}

		private static void OnSelectedItemsChanged(object dependencyObject, DependencyPropertyChangedEventArgs args)
		{
			var gridView = dependencyObject as GridView;

			if (gridView != null)
			{
				var newSelection = (IList<object>)args.NewValue;

				gridView.UpdateSelection(newSelection);
			}
		}

		private void OnSelectedItemChanged(object oldSelectedItem, object selectedItem)
		{
			UpdateSelection(new[] { selectedItem });
		}

		private void UpdateSelection(IList<object> newSelection)
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
		}

		protected virtual void SetupItemClickListener()
		{
			_clickRegistration.Disposable = Disposable.Create(() => base.ItemClick -= OnItemClick);

			base.ItemClick += OnItemClick;
		}

		private void OnItemClick(object sender, ItemClickEventArgs args)
		{
			ItemClick?.Invoke(this, new Windows.UI.Xaml.Controls.ItemClickEventArgs { ClickedItem = BindableAdapter.GetRawItem(args.Position) });

			HandleItemSelection(args);
		}

		private void HandleItemSelection(ItemClickEventArgs args)
		{
			if (SelectionMode != ListViewSelectionMode.None)
			{
				var newSelection = BindableAdapter.GetRawItem(args.Position);

				switch (SelectionMode)
				{
					case ListViewSelectionMode.Single:
						var selectedItem = BindableAdapter.SelectedItems.FirstOrDefault();

						// Unselect the current item only if a new selection is made or
						// the option to unselect the current item is activated.
						if (selectedItem != null && (selectedItem != newSelection || UnselectOnClick))
						{
							BindableAdapter.SetItemSelection(selectedItem, null, false);
						}

						if (selectedItem != newSelection)
						{
							BindableAdapter.SetItemSelection(
								newSelection,
								(args.View as ItemContainerHolder)?.Child as SelectorItem,
								true
							);
						}

						SelectedItem = newSelection;
						break;

					case ListViewSelectionMode.Multiple:
						BindableAdapter.SetItemSelection(
							BindableAdapter.GetRawItem(args.Position),
							args.View as SelectorItem,
							!BindableAdapter.SelectedItems.Contains(newSelection)
						);
						break;
				}

				SelectedItems = BindableAdapter.SelectedItems.ToArray();
			}
		}

		protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
		{
			var availableSize = new Size(
				ViewHelper.MeasureSpecGetSize(widthMeasureSpec),
				ViewHelper.MeasureSpecGetSize(heightMeasureSpec)
			);

			UpdateColumns(availableSize);

			var heightMode = ViewHelper.MeasureSpecGetMode(heightMeasureSpec);
			if (heightMode == MeasureSpecMode.Unspecified)
			{
				// By default, given an Unspecified available height, the GridView will be measured to take the height of a single row.
				// Therefore, we need the code below to ensure the GridView takes the height of all its items.
				heightMeasureSpec = ViewHelper.MakeMeasureSpec(int.MaxValue, MeasureSpecMode.AtMost);
			}

			base.OnMeasure(widthMeasureSpec, heightMeasureSpec);
		}

		private void UpdateColumns(Size availableSize)
		{
			var itemMaxWidth = GetActualItemMaxWidth(availableSize);

			var newColumnCount = itemMaxWidth > 0
				? availableSize.Width / itemMaxWidth
				: NumColumns;

			var newColumnWidth = newColumnCount > 0
				? availableSize.Width / newColumnCount
				: ColumnWidth;

			if (newColumnCount != NumColumns)
			{
				SetNumColumns(newColumnCount);
			}

			if (newColumnWidth != ColumnWidth)
			{
				SetColumnWidth(newColumnWidth);
			}
		}

		partial void OnLoadedPartial()
		{
			SetupItemClickListener();
		}

		partial void OnUnloadedPartial()
		{
			_clickRegistration.Disposable = null;
		}
	}
}
