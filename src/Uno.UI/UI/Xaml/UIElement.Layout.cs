#if !__NETSTD_REFERENCE__
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Uno.Foundation.Logging;
using Uno.UI;
using Uno.UI.Extensions;
using Uno.UI.Xaml;
using Windows.Foundation;

#if __ANDROID__
using View = Android.Views.View;
#elif __IOS__
using UIKit;
using View = UIKit.UIView;
#elif __MACOS__
using AppKit;
using View = AppKit.NSView;
#else
using View = Microsoft.UI.Xaml.UIElement;
#endif

namespace Microsoft.UI.Xaml
{
	public partial class UIElement : DependencyObject
	{
		/// <summary>
		/// When set, measure and invalidate requests will not be propagated further up the visual tree, ie they won't trigger a re-layout.
		/// Used where repeated unnecessary measure/arrange passes would be unacceptable for performance (eg scrolling in a list).
		/// </summary>
		internal bool ShouldInterceptInvalidate { get; set; }

		// In WinUI, this is in LayoutManager. For Uno, we make it static in FE for now.
		private protected static bool IsInNonClippingTree { get; set; }

		public void InvalidateMeasure()
		{
			if (ShouldInterceptInvalidate || IsMeasureDirty || IsLayoutFlagSet(LayoutFlag.MeasuringSelf))
			{
				return;
			}

			if (_traceLayoutCycle && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"[LayoutCycleTracing] InvalidateMeasure {this},{this.GetDebugName()}");
			}

			SetLayoutFlags(LayoutFlag.MeasureDirty);

			if (FeatureConfiguration.UIElement.UseInvalidateMeasurePath && !IsMeasureDirtyPathDisabled)
			{
				InvalidateParentMeasureDirtyPath();
			}
			else
			{
				(this.GetParent() as UIElement)?.InvalidateMeasure();
				if (IsVisualTreeRoot)
				{
					XamlRoot.InvalidateMeasure();
				}
			}

#if __ANDROID__ || __IOS__ || __MACOS__
			OnInvalidateMeasure();
#endif
		}

#if __ANDROID__ || __IOS__ || __MACOS__
		protected internal virtual void OnInvalidateMeasure()
		{
		}
#endif

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InvalidateMeasureDirtyPath()
		{
			if (IsMeasureDirtyOrMeasureDirtyPath)
			{
				return; // Already invalidated
			}

			SetLayoutFlags(LayoutFlag.MeasureDirtyPath);

			InvalidateParentMeasureDirtyPath();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InvalidateParentMeasureDirtyPath()
		{
			if (this.GetParent() is UIElement parent) //TODO: Should this use VisualTree.GetParent as fallback? https://github.com/unoplatform/uno/issues/8978
			{
				parent.InvalidateMeasureDirtyPath();
			}
#if __ANDROID__ || __IOS__ || __MACOS__
			else if (!IsVisualTreeRoot && this.FindParent() is { } nativeParent)
			{
				while (nativeParent is not null && nativeParent is not UIElement)
				{
					LayoutInformation.SetMeasureDirtyPath(nativeParent, true);
					nativeParent = nativeParent.FindParent();
				}

				if (nativeParent is UIElement uiElement)
				{
					uiElement.InvalidateMeasureDirtyPath();
				}
			}
#endif
			else if (IsVisualTreeRoot)
			{
				XamlRoot.InvalidateMeasure();
			}
		}

		public void InvalidateArrange()
		{
			if (ShouldInterceptInvalidate)
			{
				return;
			}

			if (IsArrangeDirty)
			{
				return; // Already dirty
			}

			if (_traceLayoutCycle)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().LogError($"[LayoutCycleTracing] InvalidateArrange {this},{this.GetDebugName()}");
				}

				if (this.Log().IsEnabled(LogLevel.Trace))
				{
					this.Log().Trace($"[LayoutCycleTracing] {Environment.StackTrace}");
				}
			}

			SetLayoutFlags(LayoutFlag.ArrangeDirty);

			if (FeatureConfiguration.UIElement.UseInvalidateArrangePath && !IsArrangeDirtyPathDisabled)
			{
				InvalidateParentArrangeDirtyPath();
			}
			else
			{
				(this.GetParent() as UIElement)?.InvalidateArrange();
				if (IsVisualTreeRoot)
				{
					XamlRoot.InvalidateArrange();
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal void InvalidateArrangeDirtyPath()
		{
			if (IsArrangeDirtyOrArrangeDirtyPath)
			{
				return; // Already invalidated
			}

			SetLayoutFlags(LayoutFlag.ArrangeDirtyPath);

			InvalidateParentArrangeDirtyPath();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void InvalidateParentArrangeDirtyPath()
		{
			if (this.GetParent() is UIElement parent) //TODO: Should this use VisualTree.GetParent as fallback? https://github.com/unoplatform/uno/issues/8978
			{
				parent.InvalidateArrangeDirtyPath();
			}
#if __ANDROID__ || __IOS__ || __MACOS__
			else if (!IsVisualTreeRoot && this.FindParent() is { } nativeParent && this is not PopupRoot)
			{
				while (nativeParent is not null && nativeParent is not UIElement)
				{
					LayoutInformation.SetArrangeDirtyPath(nativeParent, true);
					nativeParent = nativeParent.FindParent();
				}

				if (nativeParent is UIElement uiElement)
				{
					uiElement.InvalidateArrangeDirtyPath();
				}
			}
#endif
			else //TODO: Why not check IsVisualTreeRoot as in InvalidateParentMeasureDirtyPath?
			{
				XamlRoot?.InvalidateArrange();
			}
		}

		public void Measure(Size availableSize)
		{
#if __CROSSRUNTIME__
			if (!_isFrameworkElement)
#else
			if (this is not FrameworkElement)
#endif
			{
				return; // Only FrameworkElements are measurable
			}

			// Visibility should not be checked here. Consider the following scenario:
			// 1. A collapsed element is measured before it enters the visual tree
			// 2. Visibility changes to Visible after it enters the visual tree, which will call InvalidateMeasure
			// In this case, we want step 1 to clear the dirty flag.
			// If the flag isn't cleared in step 1, then InvalidateMeasure call in step 2 will do nothing because the
			// element is already dirty, which means the dirtiness isn't propagated up to RootVisual.
			// So, we want to go into DoMeasure, which will clear the flag.
			// Then, DoMeasure is going to early return if Visibility is collapsed.

			if (IsVisualTreeRoot)
			{
				MeasureVisualTreeRoot(availableSize);
			}
			else
			{
				// If possible we avoid the try/finally which might be costly on some platforms
				DoMeasure(availableSize);
			}

#if IS_UNIT_TESTS
			OnMeasurePartial(availableSize);
#endif
		}

#if IS_UNIT_TESTS
		partial void OnMeasurePartial(Size slotSize);
#endif

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and
		/// can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MeasureVisualTreeRoot(Size availableSize)
		{
			try
			{
				_isLayoutingVisualTreeRoot = true;
				DoMeasure(availableSize);
			}
			finally
			{
				_isLayoutingVisualTreeRoot = false;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void DoMeasure(Size availableSize)
		{
			var isFirstMeasure = !IsLayoutFlagSet(LayoutFlag.FirstMeasureDone);

			var isDirty =
				isFirstMeasure
				|| (availableSize != m_previousAvailableSize)
				|| IsMeasureDirty
				|| !FeatureConfiguration.UIElement.UseInvalidateMeasurePath // dirty_path disabled globally
				|| IsMeasureDirtyPathDisabled;

			var isMeasureDirtyPath = IsMeasureDirtyPath;

			if (!isDirty && !isMeasureDirtyPath)
			{
				return; // Nothing to do
			}

			if (isFirstMeasure)
			{
				SetLayoutFlags(LayoutFlag.FirstMeasureDone);
			}

			var remainingTries = MaxLayoutIterations;

			// remember whether we need to set this value back
			var wasInNonClippingTree = IsInNonClippingTree;
			// once entering a tree that is non-clipping, we don't get out of it
			// remember this for down-stream
			IsInNonClippingTree = IsNonClippingSubtree || wasInNonClippingTree;

			while (--remainingTries > 0)
			{
				if (isDirty)
				{
					// We must reset the flag **BEFORE** doing the actual measure, so the elements are able to re-invalidate themselves
					// TODO: This doesn't actually follow WinUI. It looks like in WinUI, the method
					// CUIElement::MeasureInternal is doing SetIsMeasureDirty(FALSE); at the end.
					// If we were able to align this to WinUI, we should remember clearing
					// the flag in the Visibility == Visibility.Collapsed case as well.
					// The Visibility condition is similar to the GetIsLayoutSuspended check in
					// WinUI, which does goto Cleanup and will call SetIsMeasureDirty(FALSE);
					ClearLayoutFlags(LayoutFlag.MeasureDirty | LayoutFlag.MeasureDirtyPath);

					var prevSize = DesiredSize;

					// The dirty flag is explicitly set on this element
#if DEBUG
					try
#endif
					{
						if (this.Visibility == Visibility.Collapsed)
						{
							m_desiredSize = default;
							RecursivelyApplyTemplateWorkaround();
							return;
						}

						SetLayoutFlags(LayoutFlag.MeasuringSelf);
						MeasureCore(availableSize);
						ClearLayoutFlags(LayoutFlag.MeasuringSelf);
						InvalidateArrange();
					}
#if DEBUG
					catch (Exception ex)
					{
#if __CROSSRUNTIME__
						_log.Error($"Error measuring {this}", ex);
#else
						_ = ex; // TODO
#endif
						throw;
					}
					finally
#endif
					{
						m_previousAvailableSize = availableSize;

						// if (!GetIsMeasureDuringArrange() && ! IsSameSize(prevSize, desiredSize) && !bInLayoutTransition)
						if (!IsLayoutFlagSet(LayoutFlag.MeasureDuringArrange) && prevSize != DesiredSize)
						{
							if (GetUIElementAdjustedParentInternal() is { } pParent)
							{
								pParent.OnChildDesiredSizeChanged(pParent);
							}
						}
					}

					break;
				}

				// isMeasureDirtyPath is always true here
				ClearLayoutFlags(LayoutFlag.MeasureDirtyPath);

				// The dirty flag is set on one of the descendents:
				// it will bypass the current element's MeasureOverride()
				// since it shouldn't produce a different result and it's
				// just a waste of precious CPU time to call it.
				var children = this.GetChildren().GetEnumerator();

				//foreach (var child in children)
				while (children.MoveNext())
				{
					if (children.Current is UIElement { IsMeasureDirtyOrMeasureDirtyPath: true } child)
					{
						// If the child is dirty (or is a path to a dirty descendant child),
						// We're remeasuring it.

						var previousDesiredSize = child.DesiredSize;
						child.EnsureLayoutStorage();
						child.Measure(child.m_previousAvailableSize);
						if (child.DesiredSize != previousDesiredSize)
						{
							isDirty = true;
							break;
						}
					}
#if __ANDROID__ || __IOS__ || __MACOS__
					else if (children.Current is View nativeView && LayoutInformation.GetMeasureDirtyPath(nativeView))
					{
						var previousDesiredSize = LayoutInformation.GetDesiredSize(nativeView);
						var newDesiredSize = MobileLayoutingHelpers.MeasureElement(nativeView, LayoutInformation.GetAvailableSize(nativeView));
						if (newDesiredSize != previousDesiredSize)
						{
							isDirty = true;
							break;
						}
					}
#endif
				}

				children.Dispose(); // no "using" operator here to prevent an implicit try-catch on Wasm

				if (isDirty)
				{
					continue;
				}

				break;
			}

			IsInNonClippingTree = wasInNonClippingTree;
		}

		internal virtual void MeasureCore(Size availableSize)
		{
			throw new NotSupportedException("UIElement doesn't implement MeasureCore. Inherit from FrameworkElement, which properly implements MeasureCore.");
		}

		internal bool ShouldApplyLayoutClipAsAncestorClip()
		{
			return this is Panel; // Restrict to Panels, to limit app-compat risk
								  //&& !GetIsScrollViewerHeader(); // Special-case:  ScrollViewer Headers, which can zoom, must scale the LayoutClip too
		}

		private void RecursivelyApplyTemplateWorkaround()
		{
			// Uno workaround. The template should NOT be applied here.
			// But, without this workaround, VerifyVisibilityChangeUpdatesCommandBarVisualState test will fail.
			// The real root cause for the test failure is that FindParentCommandBarForElement will
			// return null, that is because Uno doesn't yet properly have a "logical parent" concept.
			// We eagerly apply the template so that FindParentCommandBarForElement will
			// find the command bar through TemplatedParent
			if (this is Control thisAsControl)
			{
#if UNO_HAS_ENHANCED_LIFECYCLE
				thisAsControl.ApplyTemplate();
#else
				thisAsControl.TryCallOnApplyTemplate();
#endif

				// Update bindings to ensure resources defined
				// in visual parents get applied.
				this.UpdateResourceBindings();
			}

#if __CROSSRUNTIME__
			foreach (var child in _children)
#else
			foreach (var childView in this.GetChildren())
#endif
			{
#if !__CROSSRUNTIME__
				if (childView is UIElement child)
#endif
				{
					child.RecursivelyApplyTemplateWorkaround();
				}
			}
		}

		public void Arrange(Rect finalRect)
		{
#if __CROSSRUNTIME__
			if (!_isFrameworkElement)
#else
			if (this is not FrameworkElement)
#endif
			{
				return;
			}

			var firstArrangeDone = IsFirstArrangeDone;

			if (Visibility == Visibility.Collapsed)
			{
				m_finalRect = finalRect;
				HideVisual();
				ClearLayoutFlags(LayoutFlag.ArrangeDirty | LayoutFlag.ArrangeDirtyPath);
				return;
			}

			if (firstArrangeDone && !IsArrangeDirtyOrArrangeDirtyPath && finalRect == m_finalRect)
			{
				ClearLayoutFlags(LayoutFlag.ArrangeDirty | LayoutFlag.ArrangeDirtyPath);
				return; // Calling Arrange would be a waste of CPU time here.
			}

			if (IsVisualTreeRoot)
			{
				ArrangeVisualTreeRoot(finalRect);
			}
			else
			{
				// If possible we avoid the try/finally which might be costly on some platforms
				DoArrange(finalRect);
			}
		}

		/// <remarks>
		/// This method contains or is called by a try/catch containing method and can be significantly slower than other methods as a result on WebAssembly.
		/// See https://github.com/dotnet/runtime/issues/56309
		/// </remarks>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void ArrangeVisualTreeRoot(Rect finalRect)
		{
			try
			{
				_isLayoutingVisualTreeRoot = true;
				DoArrange(finalRect);
			}
			finally
			{
				_isLayoutingVisualTreeRoot = false;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void DoArrange(Rect finalRect)
		{
			var isFirstArrange = !IsLayoutFlagSet(LayoutFlag.FirstArrangeDone);

			var isDirty =
				isFirstArrange
				|| IsArrangeDirty
				|| finalRect != m_finalRect;

			if (!isDirty && !IsArrangeDirtyPath)
			{
				return; // Nothing do to
			}

			if (GetUseLayoutRounding())
			{
				finalRect.X = LayoutRound(finalRect.X);
				finalRect.Y = LayoutRound(finalRect.Y);
				finalRect.Width = LayoutRound(finalRect.Width);
				finalRect.Height = LayoutRound(finalRect.Height);
			}

			var remainingTries = MaxLayoutIterations;

			while (--remainingTries > 0)
			{
				if (IsMeasureDirtyOrMeasureDirtyPath)
				{
					// Uno doc: in WinUI, the flag is only set and reset if IsMeasureDirty, not IsMeasureDirtyOrMeasureDirtyPath
					SetLayoutFlags(LayoutFlag.MeasureDuringArrange);
					DoMeasure(m_previousAvailableSize);
					ClearLayoutFlags(LayoutFlag.MeasureDuringArrange);
				}

				if (isDirty)
				{
					ShowVisual();

					// We must store the updated slot before natively arranging the element,
					// so the updated value can be read by indirect code that is being invoked on arrange.
					// For instance, the EffectiveViewPort computation reads that value to detect slot changes (cf. PropagateEffectiveViewportChange)
					m_finalRect = finalRect;

					// We must reset the flag **BEFORE** doing the actual arrange, so the elements are able to re-invalidate themselves
					ClearLayoutFlags(LayoutFlag.ArrangeDirty | LayoutFlag.ArrangeDirtyPath);

					ArrangeCore(finalRect);

					SetLayoutFlags(LayoutFlag.FirstArrangeDone);

					break;
				}
				else if (IsArrangeDirtyPath)
				{
					ClearLayoutFlags(LayoutFlag.ArrangeDirtyPath);

					var children = this.GetChildren().GetEnumerator();

					while (children.MoveNext())
					{
						var child = children.Current;

						if (child is UIElement { IsArrangeDirtyOrArrangeDirtyPath: true } childAsUIElement)
						{
							var previousRenderSize = childAsUIElement.RenderSize;
							childAsUIElement.Arrange(childAsUIElement.m_finalRect);

							if (childAsUIElement.RenderSize != previousRenderSize)
							{
								isDirty = true;
								break;
							}
						}
#if __ANDROID__ || __IOS__ || __MACOS_
						else if (child is View nativeView && LayoutInformation.GetArrangeDirtyPath(nativeView))
						{
							// Using LayoutSlot as an approximation for RenderSize, which is UIElement-concept
							var previousRenderSize = LayoutInformation.GetLayoutSlot(child).Size;
							MobileLayoutingHelpers.ArrangeElement(child, finalRect);

							if (finalRect.Size != previousRenderSize)
							{
								isDirty = true;
								break;
							}
						}
#endif
					}

					children.Dispose(); // no "using" operator here to prevent an implicit try-catch on Wasm

					if (!isDirty)
					{
						break;
					}
				}
				else
				{
					break;
				}
			}

		}

		partial void HideVisual();
		partial void ShowVisual();

		internal virtual void ArrangeCore(Rect finalRect)
		{
			throw new NotSupportedException("UIElement doesn't implement ArrangeCore. Inherit from FrameworkElement, which properly implements ArrangeCore.");
		}
	}
}
#endif
