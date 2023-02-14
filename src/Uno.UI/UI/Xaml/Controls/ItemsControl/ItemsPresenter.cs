using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Uno.UI.DataBinding;
using Uno.UI;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;

#if XAMARIN_ANDROID
using Android.Widget;
using Android.Views;
using View = Android.Views.View;
#elif XAMARIN_IOS
using UIKit;
using View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml.Controls
{
	public partial class ItemsPresenter : FrameworkElement, IScrollSnapPointsInfo
	{
		protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTemplatedParentChanged(e);

			if (TemplatedParent is ItemsControl itemsControl && IsLoaded)
			{
				itemsControl.SetItemsPresenter(this);
			}
		}

		private protected override void OnLoaded()
		{
			base.OnLoaded();

			if (TemplatedParent is ItemsControl itemsControl && IsLoaded)
			{
				itemsControl.SetItemsPresenter(this);
			}
		}

		public ItemsPresenter()
		{
			// A content presenter does not propagate its own templated
			// parent. The content's TemplatedParent has already been set by the
			// content presenter to its own templated parent.

			//TODO TEMPLATED PARENT
			// PropagateTemplatedParent = false;
		}

		#region Padding DependencyProperty

		public Thickness Padding
		{
			get => (Thickness)GetValue(PaddingProperty);
			set => SetValue(PaddingProperty, value);
		}

		public static DependencyProperty PaddingProperty { get; } =
			DependencyProperty.Register(
				"Padding",
				typeof(Thickness),
				typeof(ItemsPresenter),
				new FrameworkPropertyMetadata(
					(Thickness)Thickness.Empty,
					(s, e) => ((ItemsPresenter)s)?.OnPaddingChanged((Thickness)e.OldValue, (Thickness)e.NewValue)
				)
			);

		private void OnPaddingChanged(Thickness oldValue, Thickness newValue)
		{
			this.InvalidateMeasure();
			PropagateLayoutValues();
		}

		#endregion

		internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
		{
			base.OnPropertyChanged2(args);

			if (args.Property == FrameworkElement.MinHeightProperty || args.Property == FrameworkElement.MinWidthProperty)
			{
				PropagateLayoutValues();
			}
		}

		/// <summary>
		/// Indicates whether the ItemsPresenter is actually enclosed in the scrollable area of the
		/// ItemsControl (or derived type). This is always true on Windows, but on Uno it's not the case
		/// for controls which delegate to a native implementation (eg <see cref="ListViewBase"/>).
		/// </summary>
		private bool IsWithinScrollableArea =>
#if XAMARIN && !__MACOS__
			!(_itemsPanel is NativeListViewBase);
#else
			true;
#endif

		private Thickness AppliedPadding =>
			IsWithinScrollableArea ?
				Padding :
				Thickness.Empty;

		protected override bool IsSimpleLayout => true;

		private View _itemsPanel;

		internal View Panel => _itemsPanel;

		private IScrollSnapPointsInfo SnapPointsProvider => Panel as IScrollSnapPointsInfo;

		public bool AreHorizontalSnapPointsRegular => SnapPointsProvider?.AreHorizontalSnapPointsRegular ?? false;

		public bool AreVerticalSnapPointsRegular => SnapPointsProvider?.AreVerticalSnapPointsRegular ?? false;

		internal void SetItemsPanel(View panel)
		{
			if (_itemsPanel == panel)
			{
				return;
			}

			_itemsPanel = panel;

			RemoveChildViews();

			if (_itemsPanel != null)
			{
#if XAMARIN_IOS || __MACOS__
				this.AddSubview(_itemsPanel);
#elif XAMARIN_ANDROID
				this.AddView(_itemsPanel);
#elif UNO_REFERENCE_API || NET461
				AddChild(_itemsPanel);
#endif

				PropagateLayoutValues();
			}

			this.InvalidateMeasure();
		}

		private void RemoveChildViews()
		{
#if XAMARIN_IOS || __MACOS__
			foreach (var subview in this.Subviews)
			{
				subview.RemoveFromSuperview();
			}
#elif XAMARIN_ANDROID
			this.RemoveAllViews();
#elif UNO_REFERENCE_API
			ClearChildren();
#endif
		}

		private void PropagateLayoutValues()
		{
#if XAMARIN && !__MACOS__
			var asListViewBase = _itemsPanel as NativeListViewBase;
			if (asListViewBase != null)
			{
				asListViewBase.Padding = Padding;
				asListViewBase.ItemsPresenterMinWidth = MinWidth;
				asListViewBase.ItemsPresenterMinHeight = MinHeight;
			}
#endif
		}

		protected override Size ArrangeOverride(Size finalSize)
		{
			var child = this.FindFirstChild();

			if (child != null)
			{
				var padding = AppliedPadding;

				var finalRect = new Rect(
					padding.Left,
					padding.Top,
					finalSize.Width - padding.Left - padding.Right,
					finalSize.Height - padding.Top - padding.Bottom
				);

				base.ArrangeElement(child, finalRect);
			}

			return finalSize;
		}

		protected override Size MeasureOverride(Size size)
		{
			var padding = AppliedPadding;

			var measuredSize = base.MeasureOverride(
				new Size(
					size.Width - padding.Left - padding.Right,
					size.Height - padding.Top - padding.Bottom
				)
			);

			return new Size(
				measuredSize.Width + padding.Left + padding.Right,
				measuredSize.Height + padding.Top + padding.Bottom
			);
		}

		public IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment) => SnapPointsProvider?.GetIrregularSnapPoints(orientation, alignment);

		public float GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset)
		{
			if (SnapPointsProvider == null)
			{
				throw new InvalidOperationException();
			}

			return SnapPointsProvider.GetRegularSnapPoints(orientation, alignment, out offset);
		}

		internal override bool CanHaveChildren() => true;

		internal static double OffsetToIndex(double offset) => Math.Max(0, offset - 2);

		internal static double IndexToOffset(int index) => index >= 0 ? index + 2 : 0;
	}
}
