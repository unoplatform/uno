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
using Windows.UI.Xaml.Media;

#if __ANDROID__
using Android.Widget;
using Android.Views;
using _ViewGroup = Android.Views.ViewGroup;
#elif __IOS__
using Windows.UI.Xaml.Media;
using UIKit;
using _ViewGroup = UIKit.UIView;
#elif __MACOS__
using Windows.UI.Xaml.Media;
using AppKit;
using _ViewGroup = AppKit.NSView;
#else
using _ViewGroup = Windows.UI.Xaml.UIElement;
#endif

namespace Windows.UI.Xaml.Controls
{
	public partial class ItemsPresenter : FrameworkElement, IScrollSnapPointsInfo
	{
		// TODO: support for Header/Footer when inside a ListViewBase
		private bool HeaderFooterEnabled =>
#if __ANDROID__ || __IOS__
		TemplatedParent is not ListViewBase
#else
		true
#endif
		;

		internal ContentControl FooterContentControl { get; private set; }

		internal ContentControl HeaderContentControl { get; private set; }

		private Orientation Orientation =>
#if __ANDROID__ || __IOS__
			_itemsPanel is NativeListViewBase nlvb ? nlvb.NativeLayout.Orientation :
#endif
				(Panel as Panel)?.PhysicalOrientation ?? Orientation.Horizontal;

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
			if (HeaderContentControl is { })
			{
				HeaderContentControl.Content = args.NewValue;
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
			if (FooterContentControl is { })
			{
				FooterContentControl.Content = args.NewValue;
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
			if (HeaderContentControl is { })
			{
				HeaderContentControl.ContentTemplate = (DataTemplate)args.NewValue;
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
			if (FooterContentControl is { })
			{
				FooterContentControl.ContentTemplate = (DataTemplate)args.NewValue;
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
			if (HeaderContentControl is { })
			{
				HeaderContentControl.Transitions = (TransitionCollection)args.NewValue;
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
			if (FooterContentControl is { })
			{
				FooterContentControl.Transitions = (TransitionCollection)args.NewValue;
			}
		}

		protected internal override void OnTemplatedParentChanged(DependencyPropertyChangedEventArgs e)
		{
			base.OnTemplatedParentChanged(e);

			if (TemplatedParent is ItemsControl itemsControl
#if !UNO_HAS_ENHANCED_LIFECYCLE
				&& IsLoaded
#endif
				)
			{
				itemsControl.SetItemsPresenter(this);
			}
		}

#if !UNO_HAS_ENHANCED_LIFECYCLE
		private protected override void OnLoaded()
		{
			base.OnLoaded();
			if (TemplatedParent is ItemsControl itemsControl && IsLoaded)
			{
				itemsControl.SetItemsPresenter(this);
			}
		}
#endif

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
#if __ANDROID__ || __IOS__
			!(_itemsPanel is NativeListViewBase);
#else
			true;
#endif

		private Thickness AppliedPadding =>
			IsWithinScrollableArea ?
				Padding :
				Thickness.Empty;

		protected override bool IsSimpleLayout => true;

		private _ViewGroup _itemsPanel;

		internal _ViewGroup Panel => _itemsPanel;

		private IScrollSnapPointsInfo SnapPointsProvider => Panel as IScrollSnapPointsInfo;

		public bool AreHorizontalSnapPointsRegular => SnapPointsProvider?.AreHorizontalSnapPointsRegular ?? false;

		public bool AreVerticalSnapPointsRegular => SnapPointsProvider?.AreVerticalSnapPointsRegular ?? false;

		internal void SetItemsPanel(_ViewGroup panel)
		{
			if (_itemsPanel == panel)
			{
				return;
			}

			// This is only called after (or while) the header and footer are created and added to the visual tree.
			global::System.Diagnostics.Debug.Assert(!HeaderFooterEnabled || HeaderContentControl is { });

			if (_itemsPanel is { })
			{
				VisualTreeHelper.RemoveView(this, _itemsPanel);
			}

			_itemsPanel = panel;

			if (_itemsPanel != null)
			{
				if (HeaderFooterEnabled)
				{
					VisualTreeHelper.AddView(this, _itemsPanel, 1);
				}
				else
				{
					VisualTreeHelper.AddView(this, _itemsPanel);
				}

				PropagateLayoutValues();
			}

			this.InvalidateMeasure();
		}

		internal void LoadChildren(_ViewGroup panel)
		{
			if (HeaderContentControl is null && HeaderFooterEnabled)
			{
				HeaderContentControl = new ContentControl
				{
					Content = Header,
					ContentTemplate = HeaderTemplate,
					ContentTransitions = HeaderTransitions,
					VerticalContentAlignment = VerticalAlignment.Stretch,
					HorizontalContentAlignment = HorizontalAlignment.Stretch,
					IsTabStop = false
				};
			}
			if (FooterContentControl is null && HeaderFooterEnabled)
			{
				FooterContentControl = new ContentControl
				{
					Content = Footer,
					ContentTemplate = FooterTemplate,
					ContentTransitions = FooterTransitions,
					VerticalContentAlignment = VerticalAlignment.Stretch,
					HorizontalContentAlignment = HorizontalAlignment.Stretch,
					IsTabStop = false
				};
			}

			// We want FooterContentControl to be assigned before calling SetItemsPanel,
			// because it may directly cause an ArrangeOverride to be called in some cases,
			// where the value is expected to be non-null.
			if (HeaderContentControl is { }) { VisualTreeHelper.AddChild(this, HeaderContentControl); }
			SetItemsPanel(panel);
			if (FooterContentControl is { }) { VisualTreeHelper.AddChild(this, FooterContentControl); }
		}

		private void PropagateLayoutValues()
		{
#if __ANDROID__ || __IOS__
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

			var unpaddedSize = new Size(
				finalSize.Width - padding.Left - padding.Right,
				finalSize.Height - padding.Top - padding.Bottom
			);

			var childRect = new Rect(new Point(padding.Left, padding.Top), default(Size));
			var previousChildSize = 0.0;

			var collection = new[]
			{
				HeaderContentControl,
				_itemsPanel,
				FooterContentControl
			};

			foreach (var view in collection)
			{
				if (view is null)
				{
					continue;
				}

				var desiredChildSize = GetElementDesiredSize(view);

				if (isHorizontal)
				{
					childRect.X += previousChildSize;

					childRect.Width = desiredChildSize.Width;
					childRect.Height = Math.Max(unpaddedSize.Height, desiredChildSize.Height);

					if (view == _itemsPanel)
					{
						// the panel should stretch to a width big enough such that the footer is at the very right
						var footerWidth = (HeaderFooterEnabled && FooterContentControl is { }) ? GetElementDesiredSize(FooterContentControl).Width : 0;
						childRect.Width = childRect.Width.AtLeast(finalSize.Width - footerWidth - childRect.X);
					}

					previousChildSize = childRect.Width;
				}
				else
				{
					childRect.Y += previousChildSize;

					childRect.Height = desiredChildSize.Height;
					childRect.Width = Math.Max(unpaddedSize.Width, desiredChildSize.Width);

					if (view == _itemsPanel)
					{
						// the panel should stretch to a height big enough such that the footer is at the very bottom
						var footerHeight = (HeaderFooterEnabled && FooterContentControl is { }) ? GetElementDesiredSize(FooterContentControl).Height : 0;
						childRect.Height = childRect.Height.AtLeast(finalSize.Height - footerHeight - childRect.Y);
					}

					previousChildSize = childRect.Height;
				}

				ArrangeElement(view, childRect);
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

			var isHorizontal = ((Panel as Panel)?.PhysicalOrientation ?? Orientation.Horizontal) == Orientation.Horizontal;

			var desiredSize = default(Size);

			var collection = new[]
			{
				HeaderContentControl,
				_itemsPanel,
				FooterContentControl
			};

			foreach (var view in collection)
			{
				if (view is null)
				{
					continue;
				}

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
					if (!PanelIsScrollSpInfo || view == Panel)
					{
						desiredSize.Width += measuredSize.Width;
					}
					desiredSize.Height = Math.Max(desiredSize.Height, measuredSize.Height);
				}
				else
				{
					if (!PanelIsScrollSpInfo || view == Panel)
					{
						desiredSize.Height += measuredSize.Height;
					}
					desiredSize.Width = Math.Max(desiredSize.Width, measuredSize.Width);
				}
			}

			return new Size(
				desiredSize.Width + padding.Left + padding.Right,
				desiredSize.Height + padding.Top + padding.Bottom
			);
		}
		private bool PanelIsScrollSpInfo => Panel is IScrollSnapPointsInfo;

		public IReadOnlyList<float> GetIrregularSnapPoints(Orientation orientation, SnapPointsAlignment alignment)
		{
			if (orientation != Orientation || SnapPointsProvider is null)
			{
				return null;
			}

			var result = new List<float>(2 + VisualTreeHelper.GetViewGroupChildrenCount(_itemsPanel));

			var panelSnapPoints = SnapPointsProvider.GetIrregularSnapPoints(orientation, alignment);

			var hasHeader = Header != null || HeaderTemplate != null;
			var hasFooter = Footer != null || FooterTemplate != null;

			var headerRect = LayoutInformation.GetLayoutSlot(HeaderContentControl);
			var footerRect = LayoutInformation.GetLayoutSlot(FooterContentControl);

			var panelStretch = Orientation == Orientation.Horizontal ?
				footerRect.Left - headerRect.Right - GetElementDesiredSize(_itemsPanel).Width :
				footerRect.Top - headerRect.Bottom - GetElementDesiredSize(_itemsPanel).Height;

			if (orientation == Orientation.Horizontal)
			{
				switch (alignment)
				{
					case SnapPointsAlignment.Near:
						if (hasHeader)
						{
							result.Add((float)headerRect.Left);
						}
						result.AddRange(panelSnapPoints.Select(i => i + (float)headerRect.Right));
						if (hasFooter)
						{
							result.Add((float)(footerRect.Left - panelStretch));
						}
						break;
					case SnapPointsAlignment.Center:
						if (hasHeader)
						{
							result.Add((float)headerRect.GetMidX());
							result.AddRange(panelSnapPoints.Select(i => i + (float)headerRect.Right));
						}
						else
						{
							result.AddRange(panelSnapPoints);
						}

						if (hasFooter)
						{
							result.Add((float)(footerRect.GetMidX() - panelStretch));
						}
						break;
					case SnapPointsAlignment.Far:
						if (hasHeader)
						{
							result.Add((float)headerRect.Right);
							result.AddRange(panelSnapPoints.Select(i => i + (float)headerRect.Right));
						}
						else
						{
							result.AddRange(panelSnapPoints);
						}

						if (hasFooter)
						{
							result.Add((float)(footerRect.Right - panelStretch));
						}
						break;
				}
			}
			else
			{
				switch (alignment)
				{
					case SnapPointsAlignment.Near:
						if (hasHeader)
						{
							result.Add((float)headerRect.Top);
							result.AddRange(panelSnapPoints.Select(i => i + (float)headerRect.Bottom));
						}
						else
						{
							result.AddRange(panelSnapPoints);
						}

						if (hasFooter)
						{
							result.Add((float)(footerRect.Top - panelStretch));
						}
						break;
					case SnapPointsAlignment.Center:
						if (hasHeader)
						{
							result.Add((float)headerRect.GetMidY());
							result.AddRange(panelSnapPoints.Select(i => i + (float)headerRect.Bottom));
						}
						else
						{
							result.AddRange(panelSnapPoints);
						}

						if (hasFooter)
						{
							result.Add((float)(footerRect.GetMidY() - panelStretch));
						}
						break;
					case SnapPointsAlignment.Far:
						if (hasHeader)
						{
							result.Add((float)headerRect.Bottom);
							result.AddRange(panelSnapPoints.Select(i => i + (float)headerRect.Bottom));
						}
						else
						{
							result.AddRange(panelSnapPoints);
						}

						if (hasFooter)
						{
							result.Add((float)(footerRect.Bottom - panelStretch));
						}
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
