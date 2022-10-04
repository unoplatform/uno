using CoreGraphics;
using Uno.Extensions;
using Uno.UI.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Windows.Foundation;
using Uno.Foundation.Logging;
using Uno.UI;
using AppKit;
using Windows.UI.Xaml.Controls.Primitives;
using SpriteKit;

namespace Windows.UI.Xaml
{
	public partial class FrameworkElement
	{
		/// <summary>
		/// When set, measure and invalidate requests will not be propagated further up the visual tree, ie they won't trigger a relayout.
		/// Used where repeated unnecessary measure/arrange passes would be unacceptable for performance (eg scrolling in a list).
		/// </summary>
		internal bool ShouldInterceptInvalidate { get; set; }

		public override bool NeedsLayout
		{
			set
			{
				if (ShouldInterceptInvalidate)
				{
					return;
				}

				if (!_inLayoutSubviews)
				{
					base.NeedsLayout = value;
				}

				SetLayoutFlags(LayoutFlag.MeasureDirty | LayoutFlag.ArrangeDirty);

				SetSuperviewNeedsLayout();
			}
		}


		public override void Layout()
		{
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

					//if (IsArrangeDirty) // commented until the MEASURE_DIRTY_PATH is properly implemented for macOS
					{
						ClearLayoutFlags(LayoutFlag.ArrangeDirty);

						OnBeforeArrange();

						Rect finalRect;
						var parent = Superview;
						if (parent is UIElement)
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
		}

		protected internal override void OnInvalidateMeasure()
		{
			base.OnInvalidateMeasure();

			// Note that the reliance on NSView.NeedsLayout to invalidate the measure / arrange phases for
			// self and parents.NeedsLayout is set to true when NSView.Frame is different from iOS, causing
			// a chain of multiple unneeded updates for the element and its parents. OnInvalidateMeasure
			// sets NeedsLayout to true and propagates to the parent but NeedsLayout by itself does not.

			if (!IsMeasureDirty)
			{
				InvalidateMeasure();
				InvalidateArrange();

				if (Parent is FrameworkElement fe)
				{
					if (!fe.IsMeasureDirty)
					{
						fe.InvalidateMeasure();
					}
				}
				else if (Parent is IFrameworkElement ife)
				{
					ife.InvalidateMeasure();
				}
			}
		}


		public CGSize SizeThatFits(CGSize size)
		{
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
					return _lastMeasure = CGSize.Empty;
				}
			}
			finally
			{
				_inLayoutSubviews = false;
			}
		}

		public override void AddSubview(NSView view)
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
			NeedsLayout = true;

			void ApplyStylesToChildren(NSView viewInner)
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

		public override void WillRemoveSubview(NSView NSView)
		{
			base.WillRemoveSubview(NSView);
			NeedsLayout = true;
		}
	}
}
