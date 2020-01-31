using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Uno.Extensions;
using Uno.UI.Views.Controls;
using Windows.UI.Xaml.Data;
using Uno.UI.Converters;
using Uno.Client;
using System.Threading.Tasks;
using Microsoft.Practices.ServiceLocation;
using Uno.Diagnostics.Eventing;
using Uno.UI.Controls;
using Windows.UI.Core;
using System.Globalization;
using Uno.Disposables;
using Uno.Extensions.Specialized;
using Windows.Foundation;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI;
using Uno.Logging;
using Uno.UI.Extensions;
using Microsoft.Extensions.Logging;

#if XAMARIN_IOS_UNIFIED
using Foundation;
using UIKit;
using CoreGraphics;
#elif XAMARIN_IOS
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.CoreGraphics;
using CGRect = System.Drawing.RectangleF;
using nfloat = System.Single;
using CGPoint = System.Drawing.PointF;
using nint = System.Int32;
using CGSize = System.Drawing.SizeF;
#endif

namespace Windows.UI.Xaml.Controls
{
	[Bindable]
	public partial class ListViewBaseSource : UICollectionViewSource
	{
		private Dictionary<ListViewBaseInternalContainer, List<Action>> _onRecycled = new Dictionary<ListViewBaseInternalContainer, List<Action>>();

		#region Members

		/// <summary>
		/// Is the UICollectionView currently undergoing animated scrolling, either user-initiated or programmatic.
		/// </summary>
		private bool _isInAnimatedScroll;
		#endregion

		public ListViewBaseSource()
		{
		}

		#region Overrides

		public override void CellDisplayingEnded(UICollectionView collectionView, UICollectionViewCell cell, NSIndexPath indexPath)
		{
			var key = cell as ListViewBaseInternalContainer;

			if (_onRecycled.TryGetValue(key, out var actions))
			{
				foreach (var a in actions) { a(); }
				_onRecycled.Remove(key);
			}

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"CellDisplayingEnded for cell at {indexPath}");
			}
		}

		/// <summary>
		/// Queues an action to be executed when the provided viewHolder is being recycled.
		/// </summary>
		internal void RegisterForRecycled(ListViewBaseInternalContainer container, Action action)
		{
			if (!_onRecycled.TryGetValue(container, out var actions))
			{
				_onRecycled[container] = actions = new List<Action>();
			}

			actions.Add(action);
		}

		public override void WillDisplayCell(UICollectionView collectionView, UICollectionViewCell cell, NSIndexPath indexPath)
		{
			var index = Owner?.XamlParent?.GetIndexFromIndexPath(IndexPath.FromNSIndexPath(indexPath)) ?? -1;
			var container = cell as ListViewBaseInternalContainer;
			var selectorItem = container?.Content as SelectorItem;
			//Update IsSelected and multi-select state immediately before display, in case either was modified after cell was prefetched but before it became visible
			if (selectorItem != null)
			{
				selectorItem.IsSelected = Owner?.XamlParent?.IsSelected(index) ?? false;
				Owner?.XamlParent?.ApplyMultiSelectState(selectorItem);
			}

			FrameworkElement.RegisterPhaseBinding(container.Content, a => RegisterForRecycled(container, a));

			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().LogDebug($"WillDisplayCell for cell at {indexPath}");
			}
		}

		internal void SetIsAnimatedScrolling() => _isInAnimatedScroll = true;

		public override void Scrolled(UIScrollView scrollView)
		{
			InvokeOnScroll();
		}

		// Called when user starts dragging
		public override void DraggingStarted(UIScrollView scrollView)
		{
			_isInAnimatedScroll = true;
		}

		// Called when user stops dragging (lifts finger)
		public override void DraggingEnded(UIScrollView scrollView, bool willDecelerate)
		{
			if (!willDecelerate)
			{
				//No fling, send final scroll event
				OnAnimatedScrollEnded();
			}
		}

		// Called when a user-initiated fling comes to a stop
		public override void DecelerationEnded(UIScrollView scrollView)
		{
			OnAnimatedScrollEnded();
		}

		// Called at the end of a programmatic animated scroll
		public override void ScrollAnimationEnded(UIScrollView scrollView)
		{
			OnAnimatedScrollEnded();
		}

		public override void DidZoom(UIScrollView scrollView)
		{
			// Note: untested, more may be needed to support zooming. On ScrollContentPresenter we set ViewForZoomingInScrollView (not 
			// obvious what it would be in the case of a list).
			Owner.XamlParent?.ScrollViewer?.OnZoomInternal((float)Owner.ZoomScale);
		}

		private void OnAnimatedScrollEnded()
		{
			_isInAnimatedScroll = false;
			InvokeOnScroll();
		}

		private void InvokeOnScroll()
		{
			var shouldReportNegativeOffsets = Owner.XamlParent?.ScrollViewer?.ShouldReportNegativeOffsets ?? false;
			// iOS can return, eg, negative values for offset, whereas Windows never will, even for 'elastic' scrolling
			var clampedOffset = shouldReportNegativeOffsets ?
				Owner.ContentOffset :
				Owner.ContentOffset.Clamp(CGPoint.Empty, Owner.UpperScrollLimit);
			Owner.XamlParent?.ScrollViewer?.OnScrollInternal(clampedOffset.X, clampedOffset.Y, isIntermediate: _isInAnimatedScroll);
		}

		public override void WillEndDragging(UIScrollView scrollView, CGPoint velocity, ref CGPoint targetContentOffset)
		{
			// If snap points are enabled, override the target offset with the calculated snap point.
			var snapTo = Owner?.NativeLayout?.GetSnapTo(velocity, targetContentOffset);
			if (snapTo.HasValue)
			{
				targetContentOffset = snapTo.Value;
			}
		}

		#endregion
	}

	/// <summary>
	/// A hidden root item that allows the reuse of ContentControl features.
	/// </summary>
	internal class ListViewBaseInternalContainer : UICollectionViewCell
	{
		private CGSize _lastUsedSize;
		private CGSize? _measuredContentSize;
		private readonly SerialDisposable _contentChangedDisposable = new SerialDisposable();
		private bool _needsLayout = true;
		private bool _interceptSetNeedsLayout = false;

		private WeakReference<NativeListViewBase> _ownerRef;
		public NativeListViewBase Owner
		{
			get { return _ownerRef?.GetTarget(); }
			set
			{
				if (value != _ownerRef?.GetTarget())
				{
					_ownerRef = new WeakReference<NativeListViewBase>(value);
				}
			}
		}

		private Orientation ScrollOrientation => Owner.NativeLayout.ScrollOrientation;
		private bool SupportsDynamicItemSizes => Owner.NativeLayout.SupportsDynamicItemSizes;
		private ILayouter Layouter => Owner.NativeLayout.Layouter;

		protected override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				GC.ReRegisterForFinalize(this);

				Core.CoreDispatcher.Main.RunIdleAsync(_ => Dispose());
			}
			else
			{
				// We need to explicitly remove the content before being disposed
				// otherwise, the children will try to reference ContentView which 
				// will have been disposed.

				foreach (var v in ContentView.Subviews)
				{
					v.RemoveFromSuperview();
				}

				base.Dispose(disposing);
			}
		}

		public ContentControl Content
		{
			get
			{
				return /* Cache the content ?*/ContentView.Subviews.FirstOrDefault() as ContentControl;
			}
			set
			{
				using (InterceptSetNeedsLayout())
				{
					if (ContentView.Subviews.Any())
					{
						ContentView.Subviews[0].RemoveFromSuperview();
					}

					value.Frame = ContentView.Bounds;
					value.AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;

					ContentView.AddSubview(value);

					ClearMeasuredSize();
					_contentChangedDisposable.Disposable = value?.RegisterDisposablePropertyChangedCallback(ContentControl.ContentProperty, (_, __) => _measuredContentSize = null);
				}
			}
		}

		public override CGRect Frame
		{
			get => base.Frame;
			set
			{
				base.Frame = value;
				UpdateContentViewFrame();
			}
		}

		public override CGRect Bounds
		{
			get => base.Bounds;
			set
			{
				if (_measuredContentSize.HasValue)
				{
					// At some points, eg during a collection change, iOS seems to apply an outdated size even after we've updated the 
					// LayoutAttributes. Keep the good size.
					SetExtent(ref value, _measuredContentSize.Value);
				}
				base.Bounds = value;
				UpdateContentViewFrame();
			}
		}

		/// <summary>
		/// Set the ContentView's size to match the InternalContainer, this is necessary for touches to buttons inside the template to 
		/// propagate properly.
		/// </summary>
		private void UpdateContentViewFrame()
		{
			if (ContentView != null)
			{
				ContentView.Frame = Bounds;
			}
		}

		/// <summary>
		/// Clear the cell's measured size, this allows the static template size to be updated with the correct databound size.
		/// </summary>
		internal void ClearMeasuredSize() => _measuredContentSize = null;

		public override UICollectionViewLayoutAttributes PreferredLayoutAttributesFittingAttributes(UICollectionViewLayoutAttributes layoutAttributes)
		{
			if (!(((object)layoutAttributes) is UICollectionViewLayoutAttributes))
			{
				// This case happens for a yet unknown GC issue, where the layoutAttribute instance passed the current
				// method maps to another object. The repro steps are not clear, and it may be related to ListView/GridView
				// data reload.
				this.Log().Error("ApplyLayoutAttributes has been called with an invalid instance. See bug #XXX for more details.");
				return null;
			}

			if (Content == null)
			{
				this.Log().Error("Empty ListViewBaseInternalContainer content.");
				return null;
			}

			try
			{
				var size = layoutAttributes.Frame.Size;
				if (_measuredContentSize == null || size != _lastUsedSize)
				{
					_lastUsedSize = size;
					var availableSize = AdjustAvailableSize(layoutAttributes.Frame.Size.ToFoundationSize());
					if (Window != null || Owner.XamlParent.Window == null)
					{
						using (InterceptSetNeedsLayout())
						{
							_measuredContentSize = Layouter.MeasureChild(Content, availableSize);
						}
					}
					else
					{
						try
						{
							//Attach temporarily, because some Uno control (eg ItemsControl) are only measured correctly after MovedToWindow has been called
							InterceptSetNeedsLayout();
							Owner.XamlParent.AddSubview(this);
							_measuredContentSize = Layouter.MeasureChild(Content, availableSize);
						}
						finally
						{
							Owner.XamlParent.RemoveChild(this);
							AllowSetNeedsLayout();
						}
					}
					if (SupportsDynamicItemSizes ||
						layoutAttributes.RepresentedElementKind == NativeListViewBase.ListViewSectionHeaderElementKind ||
						layoutAttributes.RepresentedElementKind == NativeListViewBase.ListViewHeaderElementKind ||
						layoutAttributes.RepresentedElementKind == NativeListViewBase.ListViewFooterElementKind
					)
					{
						//Fetch layout for this item from collection view, since it may have been updated by an earlier item
						var cachedAttributes = layoutAttributes.RepresentedElementKind == null ?
							Owner.NativeLayout.LayoutAttributesForItem(layoutAttributes.IndexPath) :
							Owner.NativeLayout.LayoutAttributesForSupplementaryView((NSString)layoutAttributes.RepresentedElementKind, layoutAttributes.IndexPath);
						// cachedAttributes may be null if we have modified the collection with DeleteItems
						var frame = cachedAttributes?.Frame ?? layoutAttributes.Frame;
						SetExtent(ref frame, _measuredContentSize.Value);
						var sizesAreDifferent = frame.Size != layoutAttributes.Frame.Size;
						if (sizesAreDifferent)
						{
							if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
							{
								this.Log().Debug($"Adjusting layout attributes for item at {layoutAttributes.IndexPath}({layoutAttributes.RepresentedElementKind}), Content={Content?.Content}. Previous frame={layoutAttributes.Frame}, new frame={frame}.");
							}
							Owner.NativeLayout.HasDynamicElementSizes = true;
							this.Frame = frame;
							SetNeedsLayout();

							//If we don't do a lightweight refresh here, in some circumstances (grouping enabled?) the UICollectionView seems 
							//to use stale layoutAttributes for deciding if items should be visible, leading to them popping out of view mid-viewport.
							Owner?.NativeLayout?.RefreshLayout();
						}
						layoutAttributes.Frame = frame;
						if (sizesAreDifferent)
						{
							Owner.NativeLayout.UpdateLayoutAttributesForElement(layoutAttributes);
						}

						if (layoutAttributes.RepresentedElementKind != null)
						{
							//The UICollectionView seems to call PreferredLayoutAttributesFittingAttributes for supplementary views last. Therefore
							//we need to relayout all visible items to make sure item views are offset to account for headers and group headers.
							Owner.NativeLayout.RefreshLayout();
						}
					}
				}
			}
			catch (Exception e)
			{
				this.Log().Error($"Adjusting layout attributes for {layoutAttributes?.IndexPath?.ToString() ?? "[null]"} failed", e);
			}

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				this.Log().Debug($"Returning layout attributes for item at {layoutAttributes.IndexPath}({layoutAttributes.RepresentedElementKind}), Content={Content?.Content}, with frame {layoutAttributes.Frame}");
			}

			if (layoutAttributes.Frame != this.Frame)
			{
				if (this.Log().IsEnabled(LogLevel.Debug))
				{
					this.Log().Debug($"Overwriting Frame: Item={layoutAttributes.IndexPath}: layoutAttributes.Frame={layoutAttributes.Frame}, this.Frame={this.Frame}");
				}
				// For some reason the item's Frame can be left stuck in the wrong value in certain cases where it is changed repeatedly in 
				// response to sequential collection changes.
				Frame = layoutAttributes.Frame;
			}

			return layoutAttributes;
		}

		public void SetSuperviewNeedsLayout()
		{
			if (!_needsLayout && !_interceptSetNeedsLayout)
			{
				_needsLayout = true;
				Owner?.NativeLayout?.RefreshLayout();
				ClearMeasuredSize();
				SetNeedsLayout();
			}
		}

		public override void LayoutSubviews()
		{
			_needsLayout = false;
			var size = Bounds.Size;

			if (Content != null)
			{
				Layouter.ArrangeChild(Content, new Rect(0, 0, (float)size.Width, (float)size.Height));
			}
		}

		/// <summary>
		/// Instruct the <see cref="ListViewBaseInternalContainer"/> not to propagate layout requests to its parent list, eg because the 
		/// item is in the process of being prepared by the list.
		/// </summary>
		internal IDisposable InterceptSetNeedsLayout()
		{
			_interceptSetNeedsLayout = true;
			return Disposable.Create(AllowSetNeedsLayout);
		}

		/// <summary>
		/// Tell the container to resume propagating layout requests.
		/// </summary>
		private void AllowSetNeedsLayout() => _interceptSetNeedsLayout = false;

		/// <summary>
		/// Set available size to be infinite in the scroll direction.
		/// </summary>
		private Size AdjustAvailableSize(Size availableSize)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				availableSize.Height = double.PositiveInfinity;
			}
			else
			{
				availableSize.Width = double.PositiveInfinity;
			}
			return availableSize;
		}

		/// <summary>
		/// Set the frame's extent in the direction of scrolling.
		/// </summary>
		private void SetExtent(ref CGRect frame, CGSize targetSize)
		{
			if (ScrollOrientation == Orientation.Vertical)
			{
				frame.Height = targetSize.Height;
			}
			else
			{
				frame.Width = targetSize.Width;
			}
		}
	}

	internal partial class BlockLayout : Border
	{
		public override void SetNeedsLayout()
		{
			//Block
		}
		public override void SetSuperviewNeedsLayout()
		{
			//Block
		}
	}
}
