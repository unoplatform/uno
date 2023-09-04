using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Linq;
using Uno.UI.DataBinding;
using Uno.UI;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media.Animation;
using Uno.UI.Xaml;

#if __ANDROID__
using Android.Widget;
using Android.Views;
using View = Android.Views.View;
#elif __IOS__
using UIKit;
using View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
#else
using View = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ItemsPresenter : FrameworkElement, IScrollSnapPointsInfo
	{
		private ContentControl _headerContentControl;

		private ContentControl _footerContentControl;

		private Rect _headerRect;
		private Rect _footerRect;

		private new readonly UIElementCollection _children;

		private Orientation Orientation => (Panel as Panel)?.InternalOrientation ?? Orientation.Horizontal;

		public object Header
		{
			get => GetHeaderValue();
			set => SetHeaderValue(value);
		}

		private static object GetHeaderDefaultValue() => null;

		[GeneratedDependencyProperty(ChangedCallback = true)]
		public static DependencyProperty HeaderProperty { get; } = CreateHeaderProperty();

		private void OnHeaderChanged(DependencyPropertyChangedEventArgs args)
		{
			if (_headerContentControl is { })
			{
				_headerContentControl.Content = args.NewValue;
			}
		}

		public object Footer
		{
			get => GetFooterValue();
			set => SetFooterValue(value);
		}

		private static object GetFooterDefaultValue() => null;

		[GeneratedDependencyProperty(ChangedCallback = true)]
		public static DependencyProperty FooterProperty { get; } = CreateFooterProperty();

		private void OnFooterChanged(DependencyPropertyChangedEventArgs args)
		{
			if (_footerContentControl is { })
			{
				_footerContentControl.Content = args.NewValue;
			}
		}

		private static DataTemplate GetHeaderTemplateDefaultValue() => null;

		public DataTemplate HeaderTemplate
		{
			get => GetHeaderTemplateValue();
			set => SetHeaderTemplateValue(value);
		}

		[GeneratedDependencyProperty(ChangedCallback = true)]
		public static DependencyProperty HeaderTemplateProperty { get; } = CreateHeaderTemplateProperty();

		private void OnHeaderTemplateChanged(DependencyPropertyChangedEventArgs args)
		{
			if (_headerContentControl is { })
			{
				_headerContentControl.ContentTemplate = (DataTemplate)args.NewValue;
			}
		}

		public DataTemplate FooterTemplate
		{
			get => GetFooterTemplateValue();
			set => SetFooterTemplateValue(value);
		}

		private static DataTemplate GetFooterTemplateDefaultValue() => null;

		[GeneratedDependencyProperty(ChangedCallback = true)]
		public static DependencyProperty FooterTemplateProperty { get; } = CreateFooterTemplateProperty();

		private void OnFooterTemplateChanged(DependencyPropertyChangedEventArgs args)
		{
			if (_footerContentControl is { })
			{
				_footerContentControl.ContentTemplate = (DataTemplate)args.NewValue;
			}
		}

		public TransitionCollection HeaderTransitions
		{
			get => GetHeaderTransitionsValue();
			set => SetHeaderTransitionsValue(value);
		}

		private static TransitionCollection GetHeaderTransitionsDefaultValue() => new TransitionCollection();

		[GeneratedDependencyProperty(ChangedCallback = true)]
		public static DependencyProperty HeaderTransitionsProperty { get; } = CreateHeaderTransitionsProperty();

		private void OnHeaderTransitionsChanged(DependencyPropertyChangedEventArgs args)
		{
			if (_headerContentControl is { })
			{
				_headerContentControl.Transitions = (TransitionCollection)args.NewValue;
			}
		}

		public TransitionCollection FooterTransitions
		{
			get => GetFooterTransitionsValue();
			set => SetFooterTransitionsValue(value);
		}

		private static TransitionCollection GetFooterTransitionsDefaultValue() => new TransitionCollection();

		[GeneratedDependencyProperty(ChangedCallback = true)]
		public static DependencyProperty FooterTransitionsProperty { get; } = CreateFooterTransitionsProperty();

		private void OnFooterTransitionsChanged(DependencyPropertyChangedEventArgs args)
		{
			if (_footerContentControl is { })
			{
				_footerContentControl.Transitions = (TransitionCollection)args.NewValue;
			}
		}

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

			var index = Header is UIElement ? 1 : 0;

			if (_itemsPanel is { })
			{
				RemoveChildView(index);
			}

			_itemsPanel = panel;

			if (_itemsPanel != null)
			{
				AddChildView(_itemsPanel, index);

				PropagateLayoutValues();
			}

			this.InvalidateMeasure();
		}

		private void RemoveChildView(int index)
		{
#if __IOS__ || __MACOS__
			this.Subviews[index].RemoveFromSuperview();
#elif __ANDROID__
			// TODO: how do I remove a view at a certain index
			this.RemoveAllViews();
#elif UNO_REFERENCE_API
			RemoveChild(_children[index]);
#endif
		}

		private void AddChildView(View view, int index)
		{
#if __IOS__ || __MACOS__
			// TODO: how do I add a subview at a certain index
			this.AddSubview(view);
#elif __ANDROID__
			this.AddView(view, index);
#elif UNO_REFERENCE_API || IS_UNIT_TESTS
			AddChild(view, index);
#endif
		}

		// TODO: why do we need this?
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
			// Most of this is inspired by StackPanel's ArrangeOverride

			var isHorizontal = Orientation == Orientation.Horizontal;

			var padding = AppliedPadding;

			var childRect = new Rect(new Point(padding.Left, padding.Top), default(Size));

			var previousChildSize = 0.0;

			var count = _children.Count;

			for (var i = 0; i < count; i++)
			{
				var view = _children[i];
				var desiredChildSize = GetElementDesiredSize(view);

				if (isHorizontal)
				{
					childRect.X += previousChildSize;

					previousChildSize = desiredChildSize.Width;
					childRect.Width = desiredChildSize.Width;
					childRect.Height = Math.Max(finalSize.Height, desiredChildSize.Height);
				}
				else
				{
					childRect.Y += previousChildSize;

					previousChildSize = desiredChildSize.Height;
					childRect.Height = desiredChildSize.Height;
					childRect.Width = Math.Max(finalSize.Width, desiredChildSize.Width);
				}

				if (view == _headerContentControl)
				{
					_headerRect = childRect;
				}

				if (view == _footerContentControl)
				{
					if (isHorizontal)
					{
						childRect.X = childRect.X.AtLeast(finalSize.Width);
					}
					else
					{
						childRect.Y = childRect.Y.AtLeast(finalSize.Height);
					}

					_footerRect = childRect;
				}

				ArrangeElement(view, childRect);
			}

			if (Footer is UIElement)
			{
				var child = _children[^1];
				var desiredChildSize = GetElementDesiredSize(child);
				if (isHorizontal)
				{
					childRect.X = childRect.X.AtLeast(finalSize.Width - desiredChildSize.Width);
				}
				else
				{
					childRect.X = childRect.Y.AtLeast(finalSize.Height - desiredChildSize.Height);
				}
				ArrangeElement(child, childRect);
			}

			return finalSize;
		}

		protected override Size MeasureOverride(Size size)
		{
			// Most of this is inspired by StackPanel's MeasureOverride

			var padding = AppliedPadding;

			var unpaddedSize = new Size(
				size.Width - padding.Left - padding.Right,
				size.Height - padding.Top - padding.Bottom
			);

			var isHorizontal = ((Panel as Panel)?.InternalOrientation ?? Orientation.Horizontal) == Orientation.Horizontal;

			var desiredSize = default(Size);

			var count = _children.Count;
			for (var i = 0; i < count; i++)
			{
				var view = _children[i];


				var availableSize = unpaddedSize;
				if (view != _itemsPanel)
				{
					// On Windows, every child gets an infinite length along the orientation dimension
					// except the panel, which doesn't get this treatment.
					if (isHorizontal)
					{
						availableSize.Width = float.PositiveInfinity;
					}
					else
					{
						availableSize.Height = float.PositiveInfinity;
					}
				}

				var measuredSize = MeasureElement(view, unpaddedSize);

				if (isHorizontal)
				{
					desiredSize.Width += measuredSize.Width;
					desiredSize.Height = Math.Max(desiredSize.Height, measuredSize.Height);
				}
				else
				{
					desiredSize.Width = Math.Max(desiredSize.Width, measuredSize.Width);
					desiredSize.Height += measuredSize.Height;
				}
			}

			return new Size(
				desiredSize.Width + padding.Left + padding.Right,
				desiredSize.Height + padding.Top + padding.Bottom
			);
		}

		public IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)
		{
			if (orientation != Orientation || SnapPointsProvider is null)
			{
				return null;
			}

			var result = new List<float>(2 + (Panel as Panel)?.Children.Count ?? 0);

			var panelSnapPoints = SnapPointsProvider.GetIrregularSnapPoints(orientation, alignment);

			if (orientation == Orientation.Horizontal)
			{
				switch (alignment)
				{
					case SnapPointsAlignment.Near:
						result.Add((float)_headerRect.Left);
						result.AddRange(panelSnapPoints.Select(i => i + (float)_headerRect.Right));
						result.Add((float)_footerRect.Left);
						break;
					case SnapPointsAlignment.Center:
						result.Add((float)_headerRect.GetMidX());
						result.AddRange(panelSnapPoints.Select(i => i + (float)_headerRect.Right));
						result.Add((float)_footerRect.GetMidX());
						break;
					case SnapPointsAlignment.Far:
						result.Add((float)_headerRect.Right);
						result.AddRange(panelSnapPoints.Select(i => i + (float)_headerRect.Right));
						result.Add((float)_footerRect.Right);
						break;
				}
			}
			else
			{
				switch (alignment)
				{
					case SnapPointsAlignment.Near:
						result.Add(0); // _headerRect.Top
						result.AddRange(panelSnapPoints.Select(i => i + (float)_headerRect.Bottom));
						result.Add((float)_footerRect.Top);
						break;
					case SnapPointsAlignment.Center:
						result.Add((float)_headerRect.GetMidY());
						result.AddRange(panelSnapPoints.Select(i => i + (float)_headerRect.Bottom));
						result.Add((float)_footerRect.GetMidY());
						break;
					case SnapPointsAlignment.Far:
						result.Add((float)_headerRect.Bottom);
						result.AddRange(panelSnapPoints.Select(i => i + (float)_headerRect.Bottom));
						result.Add((float)_footerRect.Bottom);
						break;
				}
			}

			return result;
		}

		public float GetRegularSnapPoints(Orientation orientation, SnapPointsAlignment alignment, out float offset) => throw new NotSupportedException("Regular snap points are not supported.");

		internal override bool CanHaveChildren() => true;

		internal static double OffsetToIndex(double offset) => Math.Max(0, offset - 2);

		internal static double IndexToOffset(int index) => index >= 0 ? index + 2 : 0;
	}
}
