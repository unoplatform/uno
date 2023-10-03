#if !__NETSTD_REFERENCE__
#nullable enable
using System;
using System.Globalization;
using System.Linq;
using Uno.Diagnostics.Eventing;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls.Primitives;

using Uno.UI;
using Uno.UI.Xaml;
using static System.Math;
using static Uno.UI.LayoutHelper;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Core.Scaling;
using Uno.UI.Extensions;

namespace Microsoft.UI.Xaml
{
	public partial class FrameworkElement
	{
		private readonly static IEventProvider _trace = Tracing.Get(FrameworkElement.TraceProvider.Id);

		private bool m_firedLoadingEvent;
		private bool m_requiresResourcesUpdate = true;

		private const double SIZE_EPSILON = 0.05d;
		private readonly Size MaxSize = new Size(double.PositiveInfinity, double.PositiveInfinity);

		private protected string DepthIndentation
		{
			get
			{
				if (Depth is int d)
				{
					return (Parent as FrameworkElement)?.DepthIndentation + $"-{d}>";
				}
				else
				{
					return "-?>";
				}
			}
		}

		partial void OnLoading();

		private void OnFwEltLoading()
		{
			IsLoading = true;

			OnLoading();
			OnLoadingPartial();

			void InvokeLoading()
			{
				_loading?.Invoke(this, new RoutedEventArgs(this));
			}

			if (FeatureConfiguration.FrameworkElement.HandleLoadUnloadExceptions)
			{
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void InvokeLoadingWithTry()
				{
					try
					{
						InvokeLoading();
					}
					catch (Exception error)
					{
						_log.Error("OnElementLoading failed in FrameworkElement", error);
						Application.Current.RaiseRecoverableUnhandledException(error);
					}
				}

				InvokeLoadingWithTry();
			}
			else
			{
				InvokeLoading();
			}
		}

		private protected sealed override void OnFwEltLoaded()
		{
			OnLoadedPartial();

			void InvokeLoaded()
			{
				// Raise event before invoking base in order to raise them top to bottom
				OnLoaded();
				_loaded?.Invoke(this, new RoutedEventArgs(this));
			}

			if (FeatureConfiguration.FrameworkElement.HandleLoadUnloadExceptions)
			{
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void InvokeLoadedWithTry()
				{
					try
					{
						InvokeLoaded();
					}
					catch (Exception error)
					{
						_log.Error("OnElementLoaded failed in FrameworkElement", error);
						Application.Current.RaiseRecoverableUnhandledException(error);
					}
				}

				InvokeLoadedWithTry();
			}
			else
			{
				InvokeLoaded();
			}
		}

		partial void OnLoadedPartial();

		internal sealed override void MeasureCore(Size availableSize)
		{
			if (_trace.IsEnabled)
			{
				/// <remarks>
				/// This method contains or is called by a try/catch containing method and
				/// can be significantly slower than other methods as a result on WebAssembly.
				/// See https://github.com/dotnet/runtime/issues/56309
				/// </remarks>
				void MeasureCoreWithTrace(Size availableSize)
				{
					var traceActivity = _trace.WriteEventActivity(
										TraceProvider.FrameworkElement_MeasureStart,
										TraceProvider.FrameworkElement_MeasureStop,
										new object[] { GetType().Name, this.GetDependencyObjectId(), Name, availableSize.ToString() }
									);

					using (traceActivity)
					{
						InnerMeasureCore(availableSize);
					}
				}

				MeasureCoreWithTrace(availableSize);
			}
			else
			{
				// This method is split in two functions to avoid the dynCalls
				// invocations generation for mono-wasm AOT inside of try/catch/finally blocks.
				InnerMeasureCore(availableSize);
			}

		}

		private protected virtual void ApplyTemplate(out bool addedVisuals)
		{
			addedVisuals = false;

			// Applying the template will not delete existing visuals. This will be done conditionally
			// when the template is invalidated.
			if (
				!HasTemplateChild() ||
				// review@xy: perhaps we can remove IsContentPresenterBypassEnabled completely, it has outlived its purpose.
				// ContentPresenter bypass causes Content to be added as direct child
				// (in situation where implicit style is not present or doesn't define a template setter),
				// preventing template application.
				// IsContentPresenterBypassEnabled depends on Template==null, which may have changed since, so we can't use that check here.
				(this as ContentControl)?.Content == GetFirstChild()
			)
			{
				var template = GetTemplate();
				if (template is not null)
				{
					// BEGIN Uno-specific
					// Try to clear the ContentPresenter bypass, because the template may generate some setup
					// that binds onto the .Content itself, and leads to situation
					// where the .Content is set as the direct child (with the bypass) for multiple parents.
					(this as ContentControl)?.ClearContentPresenterBypass();
					// END Uno-specific

					//SetIsUpdatingBindings(true);
					var child = ((IFrameworkTemplateInternal)template).LoadContent(this);

					// BEGIN Uno-specific
					if (this is Control control)
					{
						control.TemplatedRoot = child;
					}
					// END Uno-specific

					//SetIsUpdatingBindings(false);
					if (child is null)
					{
						return;
					}

					addedVisuals = true;
					AddChild(child);
				}
			}
		}

		internal void InvokeApplyTemplate(out bool addedVisuals)
		{
			ApplyTemplate(out addedVisuals);

			var pControl = this as Control;

			// Uno Specific: the implementation of InitializeStateTriggers on WinUI has an internal check that prevents
			// reapplication of state triggers if they are already initialized. That part is not ported yet, so
			// we add a check for addedVisuals here instead.
			// if (VisualTree.GetForElement(pControl) is { } visualTree)
			if (addedVisuals && VisualTree.GetForElement(pControl) is { } visualTree)
			{
				// Create VisualState StateTriggers and perform evaulation to determine initial state,
				// if we're in the visual tree (since we need it to get our qualifier context).
				// If we're not in the visual tree, we'll do this when we enter it.
				VisualStateManager.InitializeStateTriggers(pControl);
			}

			//var control = this as Control;

			if (addedVisuals)
			{
				// UNO TODO:
				//if (control is not null)
				{
					// Run all of the bindings that were created and set the
					// properties to the values from this control
					//IFC(control.RefreshTemplateBindings(TemplateBindingsRefreshType.All));
				}
				// If the object has a managed peer that is a custom type, then it might have
				// an overloaded OnApplyTemplate. Reverse P/Invoke to get that overload, if any.
				// If there's no overload, the default Control.OnApplyTemplate will be invoked,
				// which will just P/Invoke back to the native CControl::OnApplyTemplate.
				OnApplyTemplate();
			}

			// UNO TODO:
			// Update template bindings of realized element in the template.
			// This will update if element was realized after template was applied (no visuals added, hence it would not be updated earlier)
			// and if element was realized in OnApplyTemplate.
			// We should not refresh all of controls template bindings, as it could potentially overwrite values set by other ways (e.g. VSM)
			//if (control is not null &&
			//	control.NeedsTemplateBindingRefresh())
			//{
			//	control.RefreshTemplateBindings(TemplateBindingsRefreshType.WithoutInitialUpdate);
			//}

		}

		private void InnerMeasureCore(Size availableSize)
		{
			if (_traceLayoutCycle && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"[LayoutCycleTracing] Measuring {this},{this.GetDebugName()} with availableSize {availableSize}.");
			}

			// Uno TODO
			//CLayoutManager* pLayoutManager = VisualTree::GetLayoutManagerForElement(this);
			//bool bInLayoutTransition = pLayoutManager ? pLayoutManager->GetTransitioningElement() == this : false;

			Size frameworkAvailableSize = default;
			double minWidth = 0.0f;
			double maxWidth = 0.0f;
			double minHeight = 0.0f;
			double maxHeight = 0.0f;

			double clippedDesiredWidth;
			double clippedDesiredHeight;

			double marginWidth = 0.0f;
			double marginHeight = 0.0f;

			//bool bTemplateApplied = false;

			RaiseLoadingEventIfNeeded();

			//if (!bInLayoutTransition)
			{
				// Templates should be applied here.
				InvokeApplyTemplate(out _);

				// TODO: BEGIN Uno specific
				if (m_requiresResourcesUpdate && this is Control thisAsControl)
				{
					m_requiresResourcesUpdate = false;
					// Update bindings to ensure resources defined
					// in visual parents get applied.
					this.UpdateResourceBindings();
				}
				// TODO: END Uno specific

				// Subtract the margins from the available size
				var margin = Margin;
				marginWidth = margin.Left + margin.Right;
				marginHeight = margin.Top + margin.Bottom;

				// We check to see if availableSize.width and availableSize.height are finite since that will
				// also protect against NaN getting in.
				frameworkAvailableSize.Width = double.IsFinite(availableSize.Width) ? Math.Max(availableSize.Width - marginWidth, 0) : double.PositiveInfinity;
				frameworkAvailableSize.Height = double.IsFinite(availableSize.Height) ? Math.Max(availableSize.Height - marginHeight, 0) : double.PositiveInfinity;

				// Layout transforms would get processed here.

				// Adjust available size by Min/Max Width/Height

				var (minSize, maxSize) = this.GetMinMax();
				minWidth = minSize.Width;
				minHeight = minSize.Height;
				maxWidth = maxSize.Width;
				maxHeight = maxSize.Height;

				frameworkAvailableSize.Width = Math.Max(minWidth, Math.Min(frameworkAvailableSize.Width, maxWidth));
				frameworkAvailableSize.Height = Math.Max(minHeight, Math.Min(frameworkAvailableSize.Height, maxHeight));
			}
			//else
			//{
			//	// when in a transition, just take the passed in constraint without considering the above
			//	frameworkAvailableSize = availableSize;
			//}

			var desiredSize = MeasureOverride(frameworkAvailableSize);

			// We need to round now since we save the values off, and use them to determine
			// if a layout clip will be applied.
			if (GetUseLayoutRounding())
			{
				desiredSize.Width = LayoutRound(desiredSize.Width);
				desiredSize.Height = LayoutRound(desiredSize.Height);

			}

			//if (!bInLayoutTransition)
			{
				// Maximize desired size with user provided min size. It's also possible that MeasureOverride returned NaN for either
				// width or height, in which case we should use the min size as well.
				desiredSize.Width = Math.Max(desiredSize.Width, minWidth);
				if (double.IsNaN(desiredSize.Width))
				{
					desiredSize.Width = minWidth;
				}
				desiredSize.Height = Math.Max(desiredSize.Height, minHeight);
				if (double.IsNaN(desiredSize.Height))
				{
					desiredSize.Height = minHeight;
				}

				// We need to round now since we save the values off, and use them to determine
				// if a layout clip will be applied.

				if (GetUseLayoutRounding())
				{
					desiredSize.Width = LayoutRound(desiredSize.Width);
					desiredSize.Height = LayoutRound(desiredSize.Height);
				}

				// Here is the "true minimum" desired size - the one that is
				// for sure enough for the control to render its content.
				EnsureLayoutStorage();
				m_unclippedDesiredSize = desiredSize;

				// More layout transforms processing here.

				if (desiredSize.Width > maxWidth)
				{
					desiredSize.Width = maxWidth;
				}

				if (desiredSize.Height > maxHeight)
				{
					desiredSize.Height = maxHeight;
				}

				// Transform desired size to layout slot space (placeholder for when we do layout transforms)

				// Layout round the margins too. This corresponds to the behavior in ArrangeCore, where we check the unclipped desired
				// size against available space minus the rounded margin. This also prevents a bug where MeasureCore adds the unrounded
				// margin (e.g. 14) to the desired size (e.g. 55.56) and rounds the final result (69.56 rounded to 69.33 under 2.25x scale),
				// then ArrangeCore takes that rounded result (i.e. 69.33), subtracts the unrounded margin (i.e. 14) and ends up with a
				// size smaller than the desired size (69.33 - 14 = 55.33 < 55.56). This ends up putting a layout clip on an element that
				// doesn't need one, and causes big problems if the element is the scrollable extent of a carousel panel.
				double roundedMarginWidth = marginWidth;
				double roundedMarginHeight = marginHeight;
				if (GetUseLayoutRounding())
				{
					roundedMarginWidth = LayoutRound(marginWidth);
					roundedMarginHeight = LayoutRound(marginHeight);
				}

				//  Because of negative margins, clipped desired size may be negative.
				//  Need to keep it as XFLOATS for that reason and maximize with 0 at the
				//  very last point - before returning desired size to the parent.
				clippedDesiredWidth = desiredSize.Width + roundedMarginWidth;
				clippedDesiredHeight = desiredSize.Height + roundedMarginHeight;

				// only clip and constrain if the tree wants that.
				// currently only listviewitems do not want clipping
				if (!IsInNonClippingTree)
				{
					// In overconstrained scenario, parent wins and measured size of the child,
					// including any sizes set or computed, can not be larger then
					// available size. We will clip the guy later.
					if (clippedDesiredWidth > availableSize.Width)
					{
						clippedDesiredWidth = availableSize.Width;
					}

					if (clippedDesiredHeight > availableSize.Height)
					{
						clippedDesiredHeight = availableSize.Height;
					}
				}

				//  Note: unclippedDesiredSize is needed in ArrangeCore,
				//  because due to the layout protocol, arrange should be called
				//  with constraints greater or equal to child's desired size
				//  returned from MeasureOverride. But in most circumstances
				//  it is possible to reconstruct original unclipped desired size.

				desiredSize.Width = Math.Max(0, clippedDesiredWidth);
				desiredSize.Height = Math.Max(0, clippedDesiredHeight);
			}
			//else
			//{
			//	// in LT, need to take precautions
			//	desiredSize.Width = Math.Max(desiredSize.Width, 0.0f);
			//	desiredSize.Height = Math.Max(desiredSize.Height, 0.0f);
			//}

			// We need to round again in case the desired size has been modified since we originally
			// rounded it.
			if (GetUseLayoutRounding())
			{
				desiredSize.Width = LayoutRound(desiredSize.Width);
				desiredSize.Height = LayoutRound(desiredSize.Height);
			}

			if (_traceLayoutCycle && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"[LayoutCycleTracing] Measured {this},{this.GetDebugName()}: desiredSize is {desiredSize}.");
			}

			// DesiredSize must include margins
			m_desiredSize = desiredSize;

			_logDebug?.Debug($"{DepthIndentation}[{FormatDebugName()}] Measure({Name}/{availableSize}/{Margin}) = {desiredSize} _unclippedDesiredSize={m_unclippedDesiredSize}");
		}

		private void RaiseLoadingEventIfNeeded()
		{
			if (!m_firedLoadingEvent //&&
				/*ShouldRaiseEvent(_loading)*/ /*Uno TODO: Should we skip this or not? */)
			{
				//CEventManager* pEventManager = GetContext()->GetEventManager();
				//ASSERT(pEventManager);

				//TraceFrameworkElementLoadingBegin();

				// Uno specific: WinUI only raises Loading event here.
				OnFwEltLoading();
				//pEventManager->Raise(
				//	EventHandle(KnownEventIndex::FrameworkElement_Loading),
				//	FALSE /* bRefire */,
				//	this /* pSender */,
				//	NULL /* pArgs */,
				//	TRUE /* fRaiseSync */);

				//TraceFrameworkElementLoadingEnd();

				m_firedLoadingEvent = true;
			}
		}

		private string FormatDebugName()
			=> $"[{this}/{Name}";

		internal sealed override void ArrangeCore(Rect finalRect)
		{
			if (_trace.IsEnabled)
			{
				void ArrangeCoreWithTrace(Rect finalRect)
				{
#pragma warning disable CA1305 // Specify IFormatProvider
					var traceActivity = _trace.WriteEventActivity(
										TraceProvider.FrameworkElement_ArrangeStart,
										TraceProvider.FrameworkElement_ArrangeStop,
										new object[] { GetType().Name, this.GetDependencyObjectId(), Name, finalRect.ToString() }
									);
#pragma warning restore CA1305 // Specify IFormatProvider

					using (traceActivity)
					{
						InnerArrangeCore(finalRect);
					}
				}

				ArrangeCoreWithTrace(finalRect);
			}
			else
			{
				// This method is split in two functions to avoid the dynCalls
				// invocations generation for mono-wasm AOT inside of try/catch/finally blocks.
				InnerArrangeCore(finalRect);
			}

		}

		private static bool IsLessThanAndNotCloseTo(double a, double b) => a < (b - SIZE_EPSILON);

		private void InnerArrangeCore(Rect finalRect)
		{
			_logDebug?.Debug($"{DepthIndentation}{FormatDebugName()}: InnerArrangeCore({finalRect})");
			if (_traceLayoutCycle && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"[LayoutCycleTracing] Arranging {this},{this.GetDebugName()} with finalRect {finalRect}.");
			}

			// Uno TODO:
			//CLayoutManager* pLayoutManager = VisualTree::GetLayoutManagerForElement(this);
			//bool bInLayoutTransition = pLayoutManager ? pLayoutManager->GetTransitioningElement() == this : false;

			bool needsClipBounds = false;

			var arrangeSize = finalRect.Size;

			var margin = Margin;
			var marginWidth = margin.Left + margin.Right;
			var marginHeight = margin.Top + margin.Bottom;

			var ha = HorizontalAlignment;
			var va = VerticalAlignment;

			Size unclippedDesiredSize = default;
			double minWidth = 0, maxWidth = 0, minHeight = 0, maxHeight = 0;
			double effectiveMaxWidth = 0, effectiveMaxHeight = 0;

			Size oldRenderSize = default;
			Size innerInkSize = default;
			Size clippedInkSize = default;
			Size clientSize = default;
			double offsetX = 0, offsetY = 0;

			EnsureLayoutStorage();

			unclippedDesiredSize = m_unclippedDesiredSize;
			oldRenderSize = RenderSize;

			//if (!bInLayoutTransition)
			{
				Size arrangeSizeWithoutMargin = new Size(
					Math.Max(arrangeSize.Width - marginWidth, 0),
					Math.Max(arrangeSize.Height - marginHeight, 0)
				);

				var roundedMarginWidth = marginWidth;
				var roundedMarginHeight = marginHeight;

				if (GetUseLayoutRounding())
				{
					roundedMarginWidth = LayoutRound(marginWidth);
					roundedMarginHeight = LayoutRound(marginHeight);
				}

				// We handle layout rounding inconsistently across our code, this change is restricted to using layout
				// rounding for margin only on certain scenarios to avoid introducing unexpected problems.
				// If further rounding issues appear we should consider opening this behaviour to more scenarios.
				if (roundedMarginWidth != marginWidth && arrangeSizeWithoutMargin.Width != unclippedDesiredSize.Width)
				{
					double arrangeWidthWithoutRoundedMargin = Math.Max(arrangeSize.Width - roundedMarginWidth, 0);
					if (arrangeWidthWithoutRoundedMargin == unclippedDesiredSize.Width)
					{
						// The rounding difference between arrangeSizeWithoutMargin.width and unclippedDesiredSize.width
						// comes from the horizontal margin. The rounded value of that margin must be used so that this
						// FrameworkElement's ActualWidth does not return an incorrect value.
						marginWidth = roundedMarginWidth;
						arrangeSize.Width = arrangeWidthWithoutRoundedMargin;
					}
					else
					{
						arrangeSize.Width = arrangeSizeWithoutMargin.Width;
					}
				}
				else
				{
					arrangeSize.Width = arrangeSizeWithoutMargin.Width;
				}

				if (roundedMarginHeight != marginHeight && arrangeSizeWithoutMargin.Height != unclippedDesiredSize.Height)
				{
					double arrangeHeightWithoutRoundedMargin = Math.Max(arrangeSize.Height - roundedMarginHeight, 0);
					if (arrangeHeightWithoutRoundedMargin == unclippedDesiredSize.Height)
					{
						// The rounding difference between arrangeSizeWithoutMargin.height and unclippedDesiredSize.height
						// comes from the vertical margin. The rounded value of that margin must be used so that this
						// FrameworkElement's ActualHeight does not return an incorrect value.
						marginHeight = roundedMarginHeight;
						arrangeSize.Height = arrangeHeightWithoutRoundedMargin;
					}
					else
					{
						arrangeSize.Height = arrangeSizeWithoutMargin.Height;
					}
				}
				else
				{
					arrangeSize.Height = arrangeSizeWithoutMargin.Height;
				}

				if (IsLessThanAndNotCloseTo(arrangeSize.Width, unclippedDesiredSize.Width))
				{
					needsClipBounds = true;
					arrangeSize.Width = unclippedDesiredSize.Width;
				}

				if (IsLessThanAndNotCloseTo(arrangeSize.Height, unclippedDesiredSize.Height))
				{
					needsClipBounds = true;
					arrangeSize.Height = unclippedDesiredSize.Height;
				}

				// Alignment==Stretch --> arrange at the slot size minus margins
				// Alignment!=Stretch --> arrange at the unclippedDesiredSize
				if (ha != HorizontalAlignment.Stretch)
				{
					arrangeSize.Width = unclippedDesiredSize.Width;
				}

				if (va != VerticalAlignment.Stretch)
				{
					arrangeSize.Height = unclippedDesiredSize.Height;
				}

				var (minSize, maxSize) = this.GetMinMax();
				minWidth = minSize.Width;
				maxWidth = maxSize.Width;
				minHeight = minSize.Height;
				maxHeight = maxSize.Height;

				// Layout transforms processed here

				// We have to choose max between UnclippedDesiredSize and Max here, because
				// otherwise setting of max property could cause arrange at less then unclippedDS.
				// Clipping by Max is needed to limit stretch here

				effectiveMaxWidth = Math.Max(unclippedDesiredSize.Width, maxWidth);
				if (IsLessThanAndNotCloseTo(effectiveMaxWidth, arrangeSize.Width))
				{
					needsClipBounds = true;
					arrangeSize.Width = effectiveMaxWidth;
				}

				effectiveMaxHeight = Math.Max(unclippedDesiredSize.Height, maxHeight);
				if (IsLessThanAndNotCloseTo(effectiveMaxHeight, arrangeSize.Height))
				{
					needsClipBounds = true;
					arrangeSize.Height = effectiveMaxHeight;
				}
			}

			innerInkSize = ArrangeOverride(arrangeSize);

			// Here we use un-clipped InkSize because element does not know that it is
			// clipped by layout system and it shoudl have as much space to render as
			// it returned from its own ArrangeOverride
			// Inner ink size is not guaranteed to be rounded, but should be.
			// TODO: inner ink size currently only rounded if plateau > 1 to minimize impact in RC,
			// but should be consistently rounded in all plateaus.
			var scale = RootScale.GetRasterizationScaleForElement(this);
			if ((scale != 1.0f) && GetUseLayoutRounding())
			{
				innerInkSize.Width = LayoutRound(innerInkSize.Width);
				innerInkSize.Height = LayoutRound(innerInkSize.Height);
			}
			RenderSize = innerInkSize;

			//if (!IsSameSize(oldRenderSize, innerInkSize))
			//{
			//	OnActualSizeChanged();
			//}

			if (oldRenderSize != innerInkSize)
			{
				this.GetContext().EventManager.EnqueueForSizeChanged(this, oldRenderSize);
			}

			//if (!bInLayoutTransition)
			{
				// ClippedInkSize differs from InkSize only what MaxWidth/Height explicitly clip the
				// otherwise good arrangement. For ex, DS<clientSize but DS>MaxWidth - in this
				// case we should initiate clip at MaxWidth and only show Top-Left portion
				// of the element limited by Max properties. It is Top-left because in case when we
				// are clipped by container we also degrade to Top-Left, so we are consistent.
				clippedInkSize.Width = Math.Min(innerInkSize.Width, maxWidth);
				clippedInkSize.Height = Math.Min(innerInkSize.Height, maxHeight);

				// remember we have to clip if Max properties limit the inkSize
				needsClipBounds |=
					IsLessThanAndNotCloseTo(clippedInkSize.Width, innerInkSize.Width)
					|| IsLessThanAndNotCloseTo(clippedInkSize.Height, innerInkSize.Height);

				// Transform stuff here

				// Note that inkSize now can be bigger then layoutSlotSize-margin (because of layout
				// squeeze by the parent or LayoutConstrained=true, which clips desired size in Measure).

				// The client size is the size of layout slot decreased by margins.
				// This is the "window" through which we see the content of the child.
				// Alignments position ink of the child in this "window".
				// Max with 0 is necessary because layout slot may be smaller then unclipped desired size.
				clientSize.Width = Math.Max(0, finalRect.Width - marginWidth);
				clientSize.Height = Math.Max(0, finalRect.Height - marginHeight);

				// Remember we have to clip if clientSize limits the inkSize
				needsClipBounds |=
					IsLessThanAndNotCloseTo(clientSize.Width, clippedInkSize.Width)
					|| IsLessThanAndNotCloseTo(clientSize.Height, clippedInkSize.Height);

				//bool isAlignedByDirectManipulation = IsAlignedByDirectManipulation();

				//if (isAlignedByDirectManipulation)
				//{
				//	// Skip the layout engine's contribution to the element's offsets when it is already aligned by DirectManipulation.

				//	if (m_pLayoutProperties.m_horizontalAlignment == HorizontalAlignment.Stretch)
				//	{
				//		// Check if the Stretch alignment needs to be overridden with a Left alignment.
				//		// The "IsStretchHorizontalAlignmentTreatedAsLeft" case corresponds to CFrameworkElement::ComputeAlignmentOffset's "degenerate Stretch to Top-Left" branch.
				//		// The "IsFinalArrangeSizeMaximized()" case is for text controls CTextBlock, CRichTextBlock and CRichTextBlockOverflow which stretch their desired width to the finalSize argument in their ArrangeOverride method.
				//		// The "(clippedInkSize.width == clientSize.width && unclippedDesiredSize.width < clientSize.width)" case is for 3rd party controls that stretch their desired width to the final arrange width too.
				//		bool isStretchAlignmentTreatedAsNear_New =
				//			IsStretchHorizontalAlignmentTreatedAsLeft(HorizontalAlignment.Stretch, clientSize, clippedInkSize) ||
				//			(clippedInkSize.Width == clientSize.Width && unclippedDesiredSize.Width < clientSize.Width) ||
				//			IsFinalArrangeSizeMaximized();

				//		// Check if the overriding needs are changing by accessing the current status from the owning ScrollViewer control.
				//		bool isStretchAlignmentTreatedAsNear_Old = IsStretchAlignmentTreatedAsNear(true /*isForHorizontalAlignment*/);
				//		if (isStretchAlignmentTreatedAsNear_New != isStretchAlignmentTreatedAsNear_Old)
				//		{
				//			// The overriding needs are changing - push the new status to the owning ScrollViewer control.
				//			OnAlignmentChanged(true /*fIsForHorizontalAlignment*/, true/*fIsForStretchAlignment*/, isStretchAlignmentTreatedAsNear_New);
				//		}
				//	}

				//	if (m_pLayoutProperties.m_verticalAlignment == VerticalAlignment.Stretch)
				//	{
				//		// Check if the Stretch alignment needs to be overridden with a Top alignment.
				//		// The "IsStretchVerticalAlignmentTreatedAsTop" case corresponds to CFrameworkElement::ComputeAlignmentOffset's "degenerate Stretch to Top-Left" branch.
				//		// The "IsFinalArrangeSizeMaximized()" case is for text controls CTextBlock, CRichTextBlock and CRichTextBlockOverflow which stretch their desired height to the finalSize argument in their ArrangeOverride method.
				//		// The "(clippedInkSize.height == clientSize.height && unclippedDesiredSize.height < clientSize.height)" case is for 3rd party controls that stretch their desired height to the final arrange height too.
				//		bool isStretchAlignmentTreatedAsNear_New =
				//			IsStretchVerticalAlignmentTreatedAsTop(VerticalAlignment.Stretch, clientSize, clippedInkSize) ||
				//			(clippedInkSize.Height == clientSize.Height && unclippedDesiredSize.Height < clientSize.Height) ||
				//			IsFinalArrangeSizeMaximized();

				//		// Check if the overriding needs are changing by accessing the current status from the owning ScrollViewer control.
				//		bool isStretchAlignmentTreatedAsNear_Old = IsStretchAlignmentTreatedAsNear(false /*isForHorizontalAlignment*/);
				//		if (isStretchAlignmentTreatedAsNear_New != isStretchAlignmentTreatedAsNear_Old)
				//		{
				//			// The overriding needs are changing - push the new status to the owning ScrollViewer control.
				//			OnAlignmentChanged(false /*fIsForHorizontalAlignment*/, true /*fIsForStretchAlignment*/, isStretchAlignmentTreatedAsNear_New);
				//		}
				//	}
				//}
				//else
				{
					var offset = this.GetAlignmentOffset(clientSize, clippedInkSize);
					offsetX = offset.X;
					offsetY = offset.Y;
				}

				//oldOffset = VisualOffset;

				//VisualOffset.x = offsetX + finalRect.X + m_pLayoutProperties->m_margin.left;
				//VisualOffset.y = offsetY + finalRect.Y + m_pLayoutProperties->m_margin.top;

				offsetX = offsetX + finalRect.X + margin.Left;
				offsetY = offsetY + finalRect.Y + margin.Top;

				if (GetUseLayoutRounding())
				{
					offsetX = LayoutRound(offsetX);
					offsetY = LayoutRound(offsetY);
				}
			}
			//else
			//{
			//	offsetX = finalRect.X;
			//	offsetY = finalRect.Y;
			//}

			NeedsClipToSlot = needsClipBounds;

#if __WASM__
			if (FeatureConfiguration.UIElement.AssignDOMXamlProperties)
			{
				UpdateDOMXamlProperty(nameof(NeedsClipToSlot), NeedsClipToSlot);
			}
#endif
			var visualOffset = new Point(offsetX, offsetY);
			var clippedFrame = GetClipRect(needsClipBounds, visualOffset, finalRect, new Size(maxWidth, maxHeight), margin);
			ArrangeNative(visualOffset, clippedFrame);

			if (_traceLayoutCycle && this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().LogWarning($"[LayoutCycleTracing] Arranged {this},{this.GetDebugName()}: {clippedFrame}.");
			}

			AfterArrange();
		}

		internal virtual void AfterArrange() { }

		// Part of this code originates from https://github.com/dotnet/wpf/blob/b9b48871d457fc1f78fa9526c0570dae8e34b488/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/FrameworkElement.cs#L4877
		private protected virtual Rect? GetClipRect(bool needsClipToSlot, Point visualOffset, Rect finalRect, Size maxSize, Thickness margin)
		{
			if (needsClipToSlot)
			{
				Rect clipRect = default;

				// TODO: Clip rect currently only rounded in plateau > 1 to minimize impact, but should be consistently rounded in all plateaus.
				var scale = RootScale.GetRasterizationScaleForElement(this);
				var roundClipRect = (scale != 1.0f) && GetUseLayoutRounding();

				var maxWidth = maxSize.Width;
				var maxHeight = maxSize.Height;

				// this is in element's local rendering coord system
				var inkSize = RenderSize;
				var layoutSlotSize = finalRect.Size;

				var maxWidthClip = maxSize.Width.FiniteOrDefault(inkSize.Width);
				var maxHeightClip = maxSize.Height.FiniteOrDefault(inkSize.Height);

				bool needToClipLocally;

				Size clippingSize = default;

				EnsureLayoutStorage();

				// If clipping is forced, ensure the clip is at least as small as the RenderSize.
				//if (forceClipToRenderSize)
				//{
				//	maxWidthClip = MIN(inkSize.width, maxWidthClip);
				//	maxHeightClip = MIN(inkSize.height, maxHeightClip);
				//	needToClipLocally = TRUE;
				//}
				//else
				{
					// need to clip if the computed sizes exceed MaxWidth/MaxHeight/Width/Height
					needToClipLocally = IsLessThanAndNotCloseTo(maxWidthClip, inkSize.Width)
									 || IsLessThanAndNotCloseTo(maxHeightClip, inkSize.Height);
				}

				// now lets say we already clipped by MaxWidth/MaxHeight, lets see if further clipping is needed
				inkSize.Width = Math.Min(inkSize.Width, maxWidth);
				inkSize.Height = Math.Min(inkSize.Height, maxHeight);

				// now lets say we already clipped by MaxWidth/MaxHeight, lets see if further clipping is needed
				inkSize.Width = Math.Min(inkSize.Width, maxWidth);
				inkSize.Height = Math.Min(inkSize.Height, maxHeight);

				//now see if layout slot should clip the element
				var marginWidth = margin.Left + margin.Right;
				var marginHeight = margin.Top + margin.Bottom;

				clippingSize.Width = Math.Max(0, layoutSlotSize.Width - marginWidth);
				clippingSize.Height = Math.Max(0, layoutSlotSize.Height - marginHeight);

				// With layout rounding, MinMax and RenderSize are rounded. Clip size should be rounded as well.
				if (roundClipRect)
				{
					clippingSize.Width = LayoutRound(clippingSize.Width);
					clippingSize.Height = LayoutRound(clippingSize.Height);
				}

				bool needToClipSlot = IsLessThanAndNotCloseTo(clippingSize.Width, inkSize.Width)
					|| IsLessThanAndNotCloseTo(clippingSize.Height, inkSize.Height);


				if (needToClipSlot)
				{
					// The layout clip is created from the slot size determined in the parent's coordinate space,
					// but is set on the child, meaning it's affected by the child's transform/offset and is applied in
					// the child's coordinate space.  The inverse of the offset is applied to the clip to prevent the clip's
					// position from shifting as a result of the change in coordinates.
					var offset = LayoutHelper.GetAlignmentOffset(this, clippingSize, inkSize);
					var offsetX = offset.X;
					var offsetY = offset.Y;
					if (roundClipRect)
					{
						offsetX = LayoutRound(offsetX);
						offsetY = LayoutRound(offsetY);
					}

					clipRect = new Rect(-offsetX, -offsetY, clippingSize.Width, clippingSize.Height);
					if (needToClipLocally)
					{
						clipRect = clipRect.IntersectWith(new Rect(0, 0, maxWidthClip, maxHeightClip)) ?? Rect.Empty;
					}
				}
				else if (needToClipLocally)
				{
					// In this case clipRect starts at 0, 0 and max width/height clips are rounded due
					// to RenderSize and MinMax being rounded. So clipRect is already rounded.
					clipRect.Width = maxWidthClip;
					clipRect.Height = maxHeightClip;
				}

				// if we have difference between child and parent FlowDirection
				// then we have to change origin of Clipping rectangle
				// which allows us to visually keep it at the same place
				// UNO TODO
				//pParent = GetUIElementAdjustedParentInternal(FALSE /*fPublicParentsOnly*/);
				//if (pParent && pParent->IsRightToLeft() != IsRightToLeft())
				//{
				//	clipRect.X = RenderSize.width - (clipRect.X + clipRect.Width);
				//}

				if (needToClipSlot || needToClipLocally)
				{
					if (ShouldApplyLayoutClipAsAncestorClip()
#if __WASM__
						&& RenderTransform is { } renderTransform
#endif
						)
					{
#if __SKIA__
						clipRect.X += visualOffset.X;
						clipRect.Y += visualOffset.Y;
#elif __WASM__
						clipRect.X -= renderTransform.MatrixCore.M31;
						clipRect.Y -= renderTransform.MatrixCore.M32;
#endif
					}

					return clipRect;
				}
			}

			return null;
		}

		/// <summary>
		/// Calculates and applies native arrange properties.
		/// </summary>
		/// <param name="offset">Offset of the view from its parent</param>
		/// <param name="clippedFrame">Zone to clip, if clipping is required</param>
		private void ArrangeNative(Point offset, Rect? clippedFrame)
		{
			var newRect = new Rect(offset, RenderSize);

			if (
				newRect.Width < 0
				|| newRect.Height < 0
				|| double.IsNaN(newRect.Width)
				|| double.IsNaN(newRect.Height)
				|| double.IsNaN(newRect.X)
				|| double.IsNaN(newRect.Y)
			)
			{
				throw new InvalidOperationException($"{FormatDebugName()}: Invalid frame size {newRect}. No dimension should be NaN or negative value.");
			}

#if __SKIA__
			// clippedFrame here is the one calculated by FrameworkElement.GetClipRect
			// which propagates to ContainerVisual.LayoutClip.
			// The UIElement.Clip public property isn't considered here on Skia because
			// it's propagated to Visual.Clip and is set when UIElement.Clip changes.
			ArrangeVisual(newRect, clippedFrame);
#else
			var clip = Clip;
			var clipRect = clip?.Rect;
			if (clipRect.HasValue && clip?.Transform is { } transform)
			{
				clipRect = transform.TransformBounds(clipRect.Value);
			}

			if (clipRect.HasValue || clippedFrame.HasValue)
			{
				clipRect = (clipRect ?? Rect.Infinite).IntersectWith(clippedFrame ?? Rect.Infinite);
			}

			_logDebug?.Trace($"{DepthIndentation}{FormatDebugName()}.ArrangeElementNative({newRect}, clip={clipRect} (NeedsClipToSlot={NeedsClipToSlot})");

			ArrangeVisual(newRect, clipRect);
#endif
		}

		internal override void EnterImpl(EnterParams @params, int depth)
		{
			var core = this.GetContext();

			// ---------- Uno-specific BEGIN ----------
			m_requiresResourcesUpdate = true;
			// ---------- Uno-specific END ----------

			//if (@params.IsLive && @params.CheckForResourceOverrides == false)
			//{
			//    var resources = GetResourcesNoCreate();

			//    if (resources is not null &&
			//        resources.HasPotentialOverrides())
			//    {
			//        @params.CheckForResourceOverrides = TRUE;
			//    }
			//}

			base.EnterImpl(@params, depth);

			////Check for focus chrome property.
			//if (@params.IsLive)
			//{
			//	if (Control.GetIsTemplateFocusTarget(this))
			//	{
			//		UpdateFocusAncestorsTarget(true /*shouldSet*/); //Add pointer to the Descendant
			//	}
			//}

			//// Walk the list of events (if any) to keep watch of loaded events.
			//if (@params.IsLive && m_pEventList is not null)
			//{
			//	CXcpList<REQUEST>::XCPListNode* pTemp = m_pEventList.GetHead();
			//	while (pTemp is not null)
			//	{
			//		REQUEST* pRequest = (REQUEST*)pTemp->m_pData;
			//		if (pRequest && pRequest->m_hEvent.index != KnownEventIndex::UnknownType_UnknownEvent)
			//		{
			//			if (pRequest->m_hEvent.index == KnownEventIndex::FrameworkElement_Loaded)
			//			{
			//				// Take note of the fact we added a loaded event to the event manager.
			//				core->KeepWatch(WATCH_LOADED_EVENTS);
			//			}
			//		}
			//		pTemp = pTemp->m_pNext;
			//	}
			//}

			// Apply style when element is live in the tree
			if (@params.IsLive)
			{
				//if (m_eImplicitStyleProvider == ImplicitStyleProvider::None)
				//{
				//	if (!GetStyle())
				//	{
				//		IFC_RETURN(ApplyStyle());
				//	}
				//}
				//else if (m_eImplicitStyleProvider == ImplicitStyleProvider::AppWhileNotInTree)
				//{
				//	IFC_RETURN(UpdateImplicitStyle(m_pImplicitStyle, null, /*bForceUpdate*/false, /*bUpdateChildren*/false));
				//}

				// ---------- Uno-specific BEGIN ----------
				// Apply active style and default style when we enter the visual tree, if they haven't been applied already.
				this.ApplyStyles();
				// ---------- Uno-specific END ----------
			}

			// Uno-specific
			ReconfigureViewportPropagation();

			m_firedLoadingEvent = false;
		}

		// UNO TODO: Not yet ported
		internal override void LeaveImpl(LeaveParams @params)
		{
			// The way this works on WinUI is that when an element enters the visual tree, all values
			// of properties that are marked with MetaDataPropertyInfoFlags::IsSparse and MetaDataPropertyInfoFlags::IsVisualTreeProperty
			// are entered as well.
			// The property we currently know it has an effect is Resources
			if (TryGetResources() is not null)
			{
				// Using ValuesInternal to avoid Enumerator boxing
				foreach (var resource in Resources.ValuesInternal)
				{
					if (resource is FrameworkElement resourceAsUIElement)
					{
						resourceAsUIElement.LeaveImpl(@params);
					}
				}
			}

			base.LeaveImpl(@params);

			ReconfigureViewportPropagation(isLeavingTree: true);
		}
	}
}
#endif
