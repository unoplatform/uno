#nullable enable
using Uno.Extensions;
using Uno.UI.DataBinding;
using Windows.UI.Xaml;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using Uno.Disposables;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;

using Foundation;
using UIKit;
using CoreGraphics;
using ObjCRuntime;

namespace Windows.UI.Xaml.Controls
{
	public partial class ScrollViewer : ContentControl, ICustomClippingElement
	{
		/// <summary>
		/// On iOS 10-, we set a flag on the view controller such that the CommandBar doesn't automatically affect ScrollViewer content 
		/// placement. On iOS 11+, we set this behavior on the UIScrollView itself.
		/// </summary>
		internal static bool UseContentInsetAdjustmentBehavior => UIDevice.CurrentDevice.CheckSystemVersion(11, 0);

		/// <summary>
		/// The <see cref="UIScrollView"/> which will actually scroll. Mostly this will be identical to <see cref="_presenter"/>, but if we're inside a
		/// multi-line TextBox we set it to <see cref="MultilineTextBoxView"/>.
		/// </summary>
		private IUIScrollView? _scrollableContainer;

		partial void OnApplyTemplatePartial()
		{
			SetScrollableContainer();
		}

		private partial void OnLoadedPartial()
		{
			SetScrollableContainer();
		}

		private partial void OnUnloadedPartial() { }

		private void SetScrollableContainer()
		{
			_scrollableContainer = _presenter;

			if (this.FindFirstParent<TextBox>() != null)
			{
				var multiline = ((UIView)this).FindFirstChild<MultilineTextBoxView>();
				if (multiline != null)
				{
					_scrollableContainer = multiline;
				}
			}
		}

		private (double? horizontal, double? vertical, bool disableAnimation)? _pendingChangeView;

		protected override void OnAfterArrange()
		{
			base.OnAfterArrange();

			if (_pendingChangeView is { } req)
			{
				var success = ChangeViewNative(req.horizontal, req.vertical, null, req.disableAnimation);
				if (success || !IsArrangeDirty)
				{
					_pendingChangeView = default;
				}
			}
		}

		private bool ChangeViewNative(double? horizontalOffset, double? verticalOffset, float? zoomFactor, bool disableAnimation)
		{
			if (_scrollableContainer != null)
			{
				// iOS doesn't limit the offset to the scrollable bounds by itself
				var limit = _scrollableContainer.UpperScrollLimit;
				var desiredOffsets = new global::Windows.Foundation.Point(horizontalOffset ?? HorizontalOffset, verticalOffset ?? VerticalOffset);
				var clampedOffsets = new global::Windows.Foundation.Point(Math.Clamp(desiredOffsets.X, 0, limit.X), Math.Clamp(desiredOffsets.Y, 0, limit.Y));

				var success = desiredOffsets == clampedOffsets;

				if (zoomFactor is { } zoom && _scrollableContainer.ZoomScale != zoom)
				{
					_scrollableContainer.ApplyZoomScale(zoomFactor!.Value, !disableAnimation);
				}

				if (!success && IsArrangeDirty)
				{
					// If the the requested offsets are out-of - bounds, but we actually does have our final bounds yet,
					// we allow to set the desired offsets. If needed, they will then be clamped by the OnAfterArrange().
					// This is needed to allow a ScrollTo before the SV has been layouted.

					_pendingChangeView = (horizontalOffset, verticalOffset, disableAnimation);
					_scrollableContainer.ApplyContentOffset(desiredOffsets, !disableAnimation);
				}
				else
				{
					_scrollableContainer.ApplyContentOffset(clampedOffsets, !disableAnimation);
				}

				// Return true if successfully scrolled to asked offsets
				return success;
			}

			return false;
		}

		partial void OnZoomModeChangedPartial(ZoomMode zoomMode)
		{
			// On iOS, zooming is disabled by setting Minimum/MaximumZoomScale both to 1
			switch (zoomMode)
			{
				case ZoomMode.Disabled:
				default:
					_presenter?.OnMinZoomFactorChanged(1f);
					_presenter?.OnMaxZoomFactorChanged(1f);
					break;
				case ZoomMode.Enabled:
					_presenter?.OnMinZoomFactorChanged(MinZoomFactor);
					_presenter?.OnMaxZoomFactorChanged(MaxZoomFactor);
					break;
			}
		}

		private void UpdateZoomedContentAlignment()
		{
			if (ZoomFactor != 1 && Content is IFrameworkElement fe)
			{
				double insetLeft, insetTop;
				var scaledWidth = fe.ActualWidth * ZoomFactor;
				var viewportWidth = ActualWidth;

				if (viewportWidth <= scaledWidth)
				{
					insetLeft = 0;
				}
				else
				{
					switch (fe.HorizontalAlignment)
					{
						case HorizontalAlignment.Left:
							insetLeft = 0;
							break;
						case HorizontalAlignment.Right:
							insetLeft = viewportWidth - scaledWidth;
							break;
						case HorizontalAlignment.Center:
						case HorizontalAlignment.Stretch:
							insetLeft = .5 * (viewportWidth - scaledWidth);
							break;
						default:
							throw new InvalidOperationException();
					}
				}

				var scaledHeight = fe.ActualHeight * ZoomFactor;
				var viewportHeight = ActualHeight;

				if (viewportHeight <= scaledHeight)
				{
					insetTop = 0;
				}
				else
				{
					switch (fe.VerticalAlignment)
					{
						case VerticalAlignment.Top:
							insetTop = 0;
							break;
						case VerticalAlignment.Bottom:
							insetTop = viewportHeight - scaledHeight;
							break;
						case VerticalAlignment.Center:
						case VerticalAlignment.Stretch:
							insetTop = .5 * (viewportHeight - scaledHeight);
							break;
						default:
							throw new InvalidOperationException();
					}
				}

				if (_presenter != null)
				{
					_presenter.ContentInset = new UIEdgeInsets((nfloat)insetTop, (nfloat)insetLeft, 0, 0);
				}
			}
		}

		public override void WillMoveToSuperview(UIView newsuper)
		{
			base.WillMoveToSuperview(newsuper);
			UpdateSizeChangedSubscription(isCleanupRequired: newsuper == null);
		}

		bool ICustomClippingElement.AllowClippingToLayoutSlot => true;
		bool ICustomClippingElement.ForceClippingToLayoutSlot => true; // force scrollviewer to always clip
	}
}
