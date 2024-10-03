using CoreGraphics;
using Uno.Extensions;
using Uno.UI.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using UIKit;
using System.ComponentModel;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.UI.Xaml.Controls.Layouter;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement
	{
#if UNO_USES_LAYOUTER
		/// <summary>
		/// When set, measure and invalidate requests will not be propagated further up the visual tree, ie they won't trigger a relayout.
		/// Used where repeated unnecessary measure/arrange passes would be unacceptable for performance (eg scrolling in a list).
		/// </summary>
		internal bool ShouldInterceptInvalidate { get; set; }
#endif

		public override void SetNeedsLayout()
		{
#if !UNO_USES_LAYOUTER
			base.SetNeedsLayout();
#else
			if (ShouldInterceptInvalidate)
			{
				return;
			}

			if (!_inLayoutSubviews)
			{
				base.SetNeedsLayout();
			}

			SetLayoutFlags(LayoutFlag.MeasureDirty | LayoutFlag.ArrangeDirty);

			if (FeatureConfiguration.FrameworkElement.IOsAllowSuperviewNeedsLayoutWhileInLayoutSubViews || !_inLayoutSubviews)
			{
				SetSuperviewNeedsLayout();
			}
#endif
		}

		public override void LayoutSubviews()
		{
#if !UNO_USES_LAYOUTER
			base.LayoutSubviews();

			if (!IsVisualTreeRoot && !_isSettingFrameByArrangeVisual)
			{
				// This handles native-only elements with managed child/children.
				// When the parent is native-only element, it will layout its children with the proper rect.
				// So we response to the requested bounds and do the managed arrange.
				// TODO:
				//var logical = this.Frame.PhysicalToLogicalPixels();
				//this.Arrange(logical);
			}
#else
			try
			{
				try
				{
					_inLayoutSubviews = true;

					if (IsMeasureDirty)
					{
						// Add back the Margin (which is normally 'outside' the view's bounds) - the layouter will subtract it again
						var availableSizeWithMargins = Bounds.Size.Add(Margin);
						XamlMeasure(availableSizeWithMargins);
					}

					//if (IsArrangeDirty) // commented until the MEASURE_DIRTY_PATH is properly implemented for iOS
					{
						ClearLayoutFlags(LayoutFlag.ArrangeDirty);
						OnBeforeArrange();
						Rect finalRect;
						var parent = Superview;
						if (parent is UIElement or ISetLayoutSlots)
						{
							finalRect = LayoutSlotWithMarginsAndAlignments;
						}
						else
						{
							// Here the "arrange" is coming from a native element,
							// so we convert those measurements to logical ones.
							finalRect = RectFromUIRect(Frame);

							// We also need to set the LayoutSlot as it was not by the parent.
							// Note: This is only an approximation of the LayoutSlot as margin and alignment might already been applied at this point.
							LayoutInformation.SetLayoutSlot(this, finalRect);
							LayoutSlotWithMarginsAndAlignments = finalRect;
						}

						_layouter.Arrange(finalRect);

						OnAfterArrange();
					}
				}
				finally
				{
					_inLayoutSubviews = false;
				}
			}
			catch (Exception e)
			{
				this.Log().Error($"Layout failed in {GetType()}", e);
			}
#endif
		}

		public override CGSize SizeThatFits(CGSize size)
		{
#if !UNO_USES_LAYOUTER
			if (IsVisualTreeRoot)
			{
				return base.SizeThatFits(size);
			}

			var availableSize = ViewHelper.PhysicalToLogicalPixels(size);
			this.Measure(availableSize);
			return this.DesiredSize.LogicalToPhysicalPixels();
#else
			try
			{
				_inLayoutSubviews = true;

				var xamlMeasure = XamlMeasure(size);

				if (xamlMeasure != null)
				{
					return _lastMeasure = xamlMeasure.Value;
				}
				else
				{
					return _lastMeasure = base.SizeThatFits(size);
				}
			}
			finally
			{
				_inLayoutSubviews = false;
			}
#endif
		}

		public override void AddSubview(UIView view)
		{
			if (IsLoaded)
			{
				// Apply styles in the subtree being loaded (if not already applied). We do it in this way to force Styles application in a
				// 'root-first' order, because on iOS the native loading callback is raised 'leaf first,' and waiting until this point to
				// apply the style can cause Loading/Loaded to be raised twice for some views (because template of outer control changes).
				//
				// This override can be removed when Loading/Loaded timing is adjusted to fully match UWP.
				if (view is IDependencyObjectStoreProvider provider)
				{
					// Set parent so implicit styles in the tree can be resolved
					provider.Store.Parent = this;
				}
				ApplyStylesToChildren(view);
			}
			base.AddSubview(view);

			void ApplyStylesToChildren(UIView viewInner)
			{
				if (viewInner is FrameworkElement fe)
				{
					fe.ApplyStyles();
				}

				foreach (var subview in viewInner.Subviews)
				{
					ApplyStylesToChildren(subview);
				}
			}
		}
	}
}
