// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusManager.h, FocusManager.cpp

#nullable enable

using System;
using System.Threading.Tasks;
using DirectUI;

using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Input;
using Windows.Foundation;
using Microsoft.UI.Xaml.Controls;
using WinUICoreServices = Uno.UI.Xaml.Core.CoreServices;

namespace Microsoft.UI.Xaml.Input
{
	public partial class FocusManager
	{

		private static void ConvertOptionsRectsToPhysical(float scale, XYFocusOptions xyFocusOptions)
		{
			if (xyFocusOptions.ExclusionRect != null)
			{
				Rect exclusionRect = xyFocusOptions.ExclusionRect.Value;
				exclusionRect = DXamlCore.Current.DipsToPhysicalPixels(scale, exclusionRect);
				xyFocusOptions.ExclusionRect = exclusionRect;
			}

			if (xyFocusOptions.FocusHintRectangle != null)
			{
				Rect hintRect = xyFocusOptions.FocusHintRectangle.Value;
				hintRect = DXamlCore.Current.DipsToPhysicalPixels(scale, hintRect);
				xyFocusOptions.FocusHintRectangle = hintRect;
			}
		}

		private static bool InIslandsMode() => WinUICoreServices.Instance.InitializationType == InitializationType.IslandsOnly;

		private static object? FindNextFocus(
			FocusNavigationDirection focusNavigationDirection,
			XYFocusOptions xyFocusOptions)
		{
			if (focusNavigationDirection == FocusNavigationDirection.None)
			{
				throw new ArgumentOutOfRangeException(nameof(focusNavigationDirection));
			}

			var core = DXamlCore.Current;
			if (core is null)
			{
				throw new InvalidOperationException("XamlCore is not set.");
			}

			FocusManager? focusManager = null;

			if (xyFocusOptions.SearchRoot is DependencyObject searchRoot)
			{
				focusManager = VisualTree.GetFocusManagerForElement(searchRoot);
			}
			else if (core.GetHandle().InitializationType == InitializationType.IslandsOnly)
			{
				// Return error if searchRoot is not valid in islands/ desktop mode
				throw new ArgumentException("The search root must not be null.");
			}
			else
			{
				// For compat reasons, these FocusManager static APIs need to always use the CoreWindow as the
				// ContentRoot, so explicitly return the CoreWindow content root.
				var contentRootCoordinator = core.GetHandle().ContentRootCoordinator;
				var contentRoot = contentRootCoordinator?.CoreWindowContentRoot;

				if (contentRoot is null)
				{
					return null;
				}

				focusManager = contentRoot.FocusManager;
			}

			if (focusManager is null)
			{
				return null;
			}

			var root = focusManager.ContentRoot;
			var scale = RootScale.GetRasterizationScaleForContentRoot(root);
			ConvertOptionsRectsToPhysical(scale, xyFocusOptions);

			var candidate = focusManager.FindNextFocus(
				new FindFocusOptions(focusNavigationDirection),
				xyFocusOptions);

			return candidate;
		}

		private static object? GetFocusedElementImpl()
		{
			var core = DXamlCore.Current;
			if (core == null)
			{
				throw new InvalidOperationException("XamlCore is not set.");
			}

			// For compat reasons, these FocusManager static APIs need to always use the CoreWindow as the
			// ContentRoot, so explicitly return the CoreWindow content root.
			var contentRootCoordinator = core.GetHandle().ContentRootCoordinator;
			var contentRoot = contentRootCoordinator?.CoreWindowContentRoot;

			if (contentRoot == null)
			{
				return null;
			}

			return contentRoot.FocusManager.FocusedElement;
		}

		/// <summary>
		/// Gets the XamlRoot.Content object as seen by the app, so we can use it to compare against a parameter.
		/// This is because some of our APIs (FocusManager TryNextFocus/FindNextElement) specifically allow an app
		/// to scope a search to the XamlRoot level (by passing in XamlRoot.Content) but not for arbitrary elements.
		/// This function calls XamlRoot.Content the way the app would.
		/// </summary>
		/// <param name="contentRoot"></param>
		/// <returns></returns>
		private static DependencyObject? GetAppVisibleXamlRootContent(ContentRoot contentRoot)
		{
			var xamlRoot = contentRoot.GetOrCreateXamlRoot();

			return xamlRoot.Content;
		}

		private static bool TryMoveFocusStatic(
			FocusNavigationDirection focusNavigationDirection,
			FindNextElementOptions? pFocusNavigationOverride,
			ref IAsyncOperation<FocusMovementResult>? asyncOperation,
			bool useAsync)
		{
			DependencyObject? searchRootAsDO = null;
			Rect hintRect;
			Rect exclusionRect;
			XYFocusNavigationStrategyOverride navigationStrategyOverride;
			XYFocusOptions xyFocusOptions = new XYFocusOptions();
			var pIsFocusMoved = false;

			FindNextElementOptions? options = pFocusNavigationOverride;

			bool ignoreOcclusivity;
			FocusAsyncOperation? spFocusAsyncOperation = null;

			DXamlCore pCore = DXamlCore.Current;
			if (pCore == null)
			{
				throw new InvalidOperationException("XamlCore is not set.");
			}

			FocusManager? focusManager = null;
			ContentRoot? contentRoot = null;
			var contentRootCoordinator = pCore.GetHandle().ContentRootCoordinator;

			if (options != null)
			{
				var searchRoot = options.SearchRoot;
				exclusionRect = options.ExclusionRect;
				hintRect = options.HintRect;
				navigationStrategyOverride = options.XYFocusNavigationStrategyOverride;
				ignoreOcclusivity = options.IgnoreOcclusivity;
				searchRootAsDO = searchRoot as DependencyObject;

				if (searchRootAsDO != null)
				{
					contentRoot = VisualTree.GetContentRootForElement(searchRootAsDO);
					focusManager = contentRoot?.FocusManager;

					if (focusManager == null)
					{
						throw new InvalidOperationException("Search root is not part of the visual tree.");
					}

				}
				else if (pCore.GetHandle().InitializationType == InitializationType.IslandsOnly)
				{
					// SearchRoot must exist for islands/ desktop
					throw new ArgumentException("The search root must not be null.");
				}
				else
				{
					contentRoot = contentRootCoordinator?.CoreWindowContentRoot;
				}

				// If we are being passed in the public root visual of a XamlRoot as the SearchRoot, then override the SearchRoot to be the RootVisual.
				// This will enable searching through both the public root visual and the popup root. We will also allow Next/Prev.
				bool shouldOverrideSearchRoot =
					contentRoot != null
					&& GetAppVisibleXamlRootContent(contentRoot) == searchRootAsDO;

				if (shouldOverrideSearchRoot)
				{
					searchRootAsDO = contentRoot!.VisualTree.RootElement;
				}
				else
				{
					if (focusNavigationDirection != FocusNavigationDirection.Up &&
						focusNavigationDirection != FocusNavigationDirection.Down &&
						focusNavigationDirection != FocusNavigationDirection.Left &&
						focusNavigationDirection != FocusNavigationDirection.Right)
					{
						throw new ArgumentOutOfRangeException(
							"Focus navigation directions Next, Previous, and None " +
							"are not supported when using FindNextElementOptions");
					}
				}

				xyFocusOptions.NavigationStrategyOverride = navigationStrategyOverride;
				xyFocusOptions.IgnoreOcclusivity = ignoreOcclusivity;

				Rect exclusionRectNative = exclusionRect;
				Rect hintRectNative = hintRect;

				if (searchRootAsDO != null)
				{
					xyFocusOptions.SearchRoot = searchRootAsDO;
				}

				if (!exclusionRectNative.IsUniform)
				{
					xyFocusOptions.ExclusionRect = exclusionRectNative;
				}

				if (!hintRectNative.IsUniform)
				{
					xyFocusOptions.FocusHintRectangle = hintRectNative;
				}

				if (contentRoot != null)
				{
					var scale = RootScale.GetRasterizationScaleForContentRoot(contentRoot);
					ConvertOptionsRectsToPhysical(scale, xyFocusOptions);
				}
			}

			if (focusManager == null)
			{
				// Return error if call is without focus navigation option in islands/ desktop
				if (pCore.GetHandle().InitializationType == InitializationType.IslandsOnly)
				{
					throw new InvalidOperationException("Focus navigation options must be set for desktop apps.");
				}

				// For compat reasons, these FocusManager static APIs need to always use the CoreWindow as the
				// ContentRoot, so explicitly return the CoreWindow content root.
				if (contentRoot == null)
				{
					contentRoot = contentRootCoordinator?.CoreWindowContentRoot;
				}
				if (contentRoot == null)
				{
					return pIsFocusMoved;
				}

				focusManager = contentRoot.FocusManager;
			}

			FocusMovement movement = new FocusMovement(xyFocusOptions, focusNavigationDirection, null);

			if (useAsync)
			{
				spFocusAsyncOperation = new FocusAsyncOperation(movement.CorrelationId);
				asyncOperation = spFocusAsyncOperation.CreateAsyncOperation();

				// Async operation is not guaranteed to be released synchronously.
				// Therefore, we let UpdateFocus to handle the responsibility of releasing it.
				// TODO Uno specific: Do not use async operations, only simulated
				// movement.ShouldCompleteAsyncOperation = focusManager.TrySetAsyncOperation(spFocusAsyncOperation);

				if (movement.ShouldCompleteAsyncOperation)
				{
					//spFocusAsyncOperation.StartOperation();
				}
			}

			FocusMovementResult result = focusManager.FindAndSetNextFocus(movement);

			// TODO Uno specific: Simulate async completion.
			spFocusAsyncOperation?.CoreSetResults(result);
			spFocusAsyncOperation?.CoreFireCompletion();

			// We ignore result.GetHResult() here because this is a "Try" function
			pIsFocusMoved = result.WasMoved;

			// Async operation is not guaranteed to be released synchronously.
			// Therefore, we let UpdateFocus to handle the responsibility of releasing it.
			//spFocusAsyncOperation.Detach();

			return pIsFocusMoved;
		}

		private static IAsyncOperation<FocusMovementResult> TryMoveFocusAsyncImpl(FocusNavigationDirection focusNavigationDirection)
		{
			IAsyncOperation<FocusMovementResult>? asyncOperation = null;
			var focusMoved = TryMoveFocusStatic(focusNavigationDirection, null, ref asyncOperation, true);
			return asyncOperation ?? AsyncOperation.FromTask(ct => Task.FromResult(new FocusMovementResult()));
		}

		private static IAsyncOperation<FocusMovementResult> TryMoveFocusWithOptionsAsyncImpl(
			 FocusNavigationDirection focusNavigationDirection,
			 FindNextElementOptions focusNavigationOptions)
		{
			IAsyncOperation<FocusMovementResult>? asyncOperation = null;
			var focusMoved = TryMoveFocusStatic(focusNavigationDirection, focusNavigationOptions, ref asyncOperation, true);
			return asyncOperation ?? AsyncOperation.FromTask(ct => Task.FromResult(new FocusMovementResult()));
		}

		private static bool TryMoveFocusImpl(FocusNavigationDirection focusNavigationDirection)
		{
			IAsyncOperation<FocusMovementResult>? asyncOperation = null;
			var isFocusMoved = TryMoveFocusStatic(focusNavigationDirection, null, ref asyncOperation, false);
			return isFocusMoved;
		}

		private static bool TryMoveFocusWithOptionsImpl(
			 FocusNavigationDirection focusNavigationDirection,
			 FindNextElementOptions pFocusNavigationOverride)
		{
			IAsyncOperation<FocusMovementResult>? asyncOperation = null;
			var isFocusMoved = TryMoveFocusStatic(focusNavigationDirection, pFocusNavigationOverride, ref asyncOperation, false);
			return isFocusMoved;
		}

		private static IAsyncOperation<FocusMovementResult> TryFocusAsyncImpl(
			DependencyObject pElement,
			FocusState focusState)
		{
			FocusManager? focusManager = VisualTree.GetFocusManagerForElement(pElement);
			if (focusManager == null)
			{
				throw new InvalidOperationException("Element is not part of the visual tree.");
			}

			FocusMovement movement = new FocusMovement(pElement, FocusNavigationDirection.None, focusState);

			var spFocusAsyncOperation = new FocusAsyncOperation(movement.CorrelationId);
			var asyncOperation = spFocusAsyncOperation.CreateAsyncOperation();

			if (FocusProperties.IsFocusable(pElement) == false)
			{
				// We need to start and complete the async operation since this is a no-op

				spFocusAsyncOperation.CoreSetResults(new FocusMovementResult());
				spFocusAsyncOperation.CoreFireCompletion();
				return asyncOperation;
			}

			// TODO Uno specific: Do not use async operations, only simulated
			//movement.ShouldCompleteAsyncOperation = focusManager.TrySetAsyncOperation(spFocusAsyncOperation);
			if (movement.ShouldCompleteAsyncOperation)
			{
				//spFocusAsyncOperation.StartOperation();
			}

			var result = focusManager.SetFocusedElement(movement);

			// TODO Uno specific: Simulate async completion.
			spFocusAsyncOperation?.CoreSetResults(result);
			spFocusAsyncOperation?.CoreFireCompletion();

			// Async operation is not guaranteed to be released synchronously.
			// Therefore, we let UpdateFocus to handle the responsibility of releasing it.
			//spFocusAsyncOperation.Detach();

			return asyncOperation;
		}

		private static UIElement? FindNextFocusableElementImpl(FocusNavigationDirection focusNavigationDirection)
		{
			if (InIslandsMode())
			{
				// This api is not supported in islands/ desktop mode. 
				throw new NotSupportedException("This API is not supported in desktop mode.");
			}

			XYFocusOptions xyFocusOptions = new XYFocusOptions();
			xyFocusOptions.UpdateManifold = false;

			var candidate = FindNextFocus(focusNavigationDirection, xyFocusOptions);
			return candidate as UIElement;
		}

		private static UIElement? FindNextFocusableElementWithHintImpl(FocusNavigationDirection focusNavigationDirection, Rect focusHintRectangle)
		{
			if (InIslandsMode())
			{
				// This api is not supported in islands/ desktop mode. 
				throw new NotSupportedException("This API is not supported in desktop mode.");
			}

			Rect hintRect = focusHintRectangle;

			var xyFocusOptions = new XYFocusOptions();
			xyFocusOptions.UpdateManifold = false;
			xyFocusOptions.FocusHintRectangle = hintRect;

			var candidate = FindNextFocus(focusNavigationDirection, xyFocusOptions);
			return candidate as UIElement;
		}

		private object? FindNextFocusWithSearchRootIgnoreEngagementImpl(FocusNavigationDirection focusNavigationDirection, object? pSearchRoot)
		{
			object? spScope = pSearchRoot;
			DependencyObject? spScopeDO = spScope as DependencyObject;

			object? candidate;

			var cDOSearchRoot = spScopeDO;
			if (cDOSearchRoot == null && InIslandsMode())
			{
				// Return error if cDOSearchRoot is not valid in islands/ desktop mode
				throw new ArgumentException("Search root must not be null.", nameof(pSearchRoot));
			}

			var xyFocusOptions = new XYFocusOptions();
			xyFocusOptions.SearchRoot = cDOSearchRoot;
			xyFocusOptions.ConsiderEngagement = false;

			candidate = FindNextFocus(focusNavigationDirection, xyFocusOptions);
			return candidate;
		}

		//TODO Uno: Will be used for focus engagement.
		internal static void SetEngagedControl(object pEngagedControl)
		{
			DependencyObject? controlAsDO = pEngagedControl as DependencyObject;

			var pFocusManager = VisualTree.GetFocusManagerForElement(controlAsDO);

			if (pFocusManager?.EngagedControl != null)
			{
				// We should never engage a control when there is already
				// an engaged control.
				// TODO Uno: Test this
				throw new InvalidOperationException("Another control is already engaged.");
			}

			if (pEngagedControl != null)
			{
				var ccontrol = controlAsDO as Control;
				ccontrol?.SetValue(Control.IsFocusEngagedProperty, true);
			}
		}

		/// <summary>
		/// Sets focused element.
		/// </summary>
		/// <param name="pElement">Element to focus.</param>
		/// <param name="focusState">Requested focus state.</param>
		/// <param name="animateIfBringIntoView">Animate bring into view.</param>
		/// <param name="forceBringIntoView">Force bring into view.</param>
		/// <param name="isProcessingTab">Is tab being processed?</param>
		/// <param name="isShiftPressed">Is shift pressed?</param>
		/// <returns>True if focus successfully transitioned.</returns>
		private bool SetFocusedElement(
			 DependencyObject pElement,
			 FocusState focusState,
			 bool animateIfBringIntoView,
			 bool forceBringIntoView,
			 bool isProcessingTab,
			 bool isShiftPressed)
		{
			FocusNavigationDirection focusNavigationDirection = FocusNavigationDirection.None;
			if (isProcessingTab)
			{
				if (isShiftPressed)
				{
					focusNavigationDirection = FocusNavigationDirection.Previous;
				}
				else
				{
					focusNavigationDirection = FocusNavigationDirection.Next;
				}
			}

			bool pFocusUpdated = SetFocusedElementWithDirection(
				pElement,
				focusState,
				animateIfBringIntoView,
				forceBringIntoView,
				focusNavigationDirection);

			return pFocusUpdated;
		}

		/// <summary>
		/// Sets focused element in given direction.
		/// </summary>
		/// <param name="pElement">Element to set focus on.</param>
		/// <param name="focusState">Focus state.</param>
		/// <param name="animateIfBringIntoView">Animate bring into view.</param>
		/// <param name="forceBringIntoView">Force bring into view.</param>
		/// <param name="focusNavigationDirection">Focus direction.</param>
		/// <returns>True if focus was set.</returns>
		internal static bool SetFocusedElementWithDirection(
			 DependencyObject? pElement,
			 FocusState focusState,
			 bool animateIfBringIntoView,
			 bool forceBringIntoView,
			 FocusNavigationDirection focusNavigationDirection)
		{
			DependencyObject? spElementToFocus = pElement;
			Control? spControlToFocus;
			bool pFocusUpdated = false;

			if (pElement == null)
			{
				throw new ArgumentNullException(nameof(pElement));
			}

			spControlToFocus = spElementToFocus as Control;
			if (spControlToFocus != null)
			{
				// For control, use IControl.Focus, for safer backward compat
				if (animateIfBringIntoView)
				{
					// Set the flag that indicates that the Focus change operation
					// needs to use an animation if the element is brouhgt into view.
					(spControlToFocus as Control).SetAnimateIfBringIntoView();
				}

				pFocusUpdated = (spControlToFocus as Control).FocusWithDirection(focusState, focusNavigationDirection);
			}
			else
			{
				bool isShiftPressed = (focusNavigationDirection == FocusNavigationDirection.Previous);
				bool isProcessingTab = (focusNavigationDirection == FocusNavigationDirection.Next) || isShiftPressed;

				// Set focus on non-controls, like Hyperlink.
				DependencyObject? spElementToFocusDO;
				spElementToFocusDO = spElementToFocus as DependencyObject;
				FocusManager? focusManager = VisualTree.GetFocusManagerForElement(spElementToFocusDO);
				FocusMovement movement = new FocusMovement(
					spElementToFocusDO,
					focusNavigationDirection,
					focusState);
				movement.ForceBringIntoView = forceBringIntoView;
				movement.AnimateIfBringIntoView = animateIfBringIntoView;
				movement.IsProcessingTab = isProcessingTab;
				movement.IsShiftPressed = isShiftPressed;
				var result = focusManager?.SetFocusedElement(movement);
				pFocusUpdated = result?.WasMoved ?? false;
			}

			return pFocusUpdated;
		}

		/// <summary>
		/// Validation and forward to instance, same as next one, just different bReverse.
		/// </summary>
		/// <param name="pSearchScope">Search scope.</param>
		/// <returns>Focusable element or null.</returns>
		private static DependencyObject? FindFirstFocusableElementImpl(DependencyObject? pSearchScope)
		{
			DependencyObject? searchStartDO = pSearchScope;

			DependencyObject? element = null;

			if (searchStartDO != null)
			{
				var pFocusManager = VisualTree.GetFocusManagerForElement(searchStartDO);
				element = pFocusManager?.GetFirstFocusableElement(searchStartDO);
			}
			else
			{
				// For compat reasons, these FocusManager static APIs need to always use the CoreWindow as the
				// ContentRoot, so explicitly return the CoreWindow content root.
				var contentRoot = DXamlCore.Current.GetHandle().ContentRootCoordinator.CoreWindowContentRoot;

				if (contentRoot == null)
				{
					return null;
				}

				element = contentRoot.FocusManager.GetFirstFocusableElementFromRoot(useReverseDirection: false);
			}

			return element;
		}

		/// <summary>
		/// Validation and forward to instance, same as previous one, just reverese.
		/// </summary>
		/// <param name="pSearchScope">Search scope.</param>
		/// <returns>Focusable element or null.</returns>
		private static DependencyObject? FindLastFocusableElementImpl(DependencyObject? pSearchScope)
		{
			DependencyObject? searchStartDO = pSearchScope;

			DependencyObject? element = null;

			if (searchStartDO != null)
			{
				var pFocusManager = VisualTree.GetFocusManagerForElement(searchStartDO);
				element = pFocusManager?.GetLastFocusableElement(searchStartDO);
			}
			else
			{
				// For compat reasons, these FocusManager static APIs need to always use the CoreWindow as the
				// ContentRoot, so explicitly return the CoreWindow content root.
				var contentRoot = DXamlCore.Current.GetHandle().ContentRootCoordinator.CoreWindowContentRoot;

				if (contentRoot == null)
				{
					return null;
				}

				element = contentRoot.FocusManager.GetFirstFocusableElementFromRoot(useReverseDirection: true);
			}

			return element;
		}

		//TODO Uno: Will be used from Control.OnFocusEngaged
		internal static object? FindNextFocusWithSearchRootIgnoreEngagementWithHintRectImpl(
			FocusNavigationDirection focusNavigationDirection,
			object pSearchRoot,
			Rect focusHintRectangle,
			Rect focusExclusionRectangle)
		{
			object? spScope = pSearchRoot;
			DependencyObject? spScopeDO = spScope as DependencyObject;

			object? candidate = null;

			Rect hintRect = focusHintRectangle;
			Rect exRect = focusExclusionRectangle;

			var cDOSearchRoot = spScopeDO;
			if (cDOSearchRoot == null && InIslandsMode())
			{
				// Return error if cDOSearchRoot is not valid in islands/ desktop mode
				throw new InvalidOperationException("Search root is invalid.");
			}

			XYFocusOptions xyFocusOptions = new XYFocusOptions();
			xyFocusOptions.SearchRoot = cDOSearchRoot;
			xyFocusOptions.ConsiderEngagement = false;
			xyFocusOptions.FocusHintRectangle = hintRect;
			xyFocusOptions.ExclusionRect = exRect;

			candidate = FindNextFocus(focusNavigationDirection, xyFocusOptions);
			return candidate;
		}

		internal static object? FindNextFocusWithSearchRootIgnoreEngagementWithClipImpl(
			FocusNavigationDirection focusNavigationDirection,
			object pSearchRoot,
			bool ignoreClipping,
			bool ignoreCone)
		{
			object? spScope = pSearchRoot;
			DependencyObject? spScopeDO = spScope as DependencyObject;

			object? candidate = null;

			var cDOSearchRoot = spScopeDO as DependencyObject;
			if (cDOSearchRoot == null && InIslandsMode())
			{
				// Return error if cDOSearchRoot is not valid in islands/ desktop mode
				throw new InvalidOperationException("Invalid search root");
			}

			XYFocusOptions xyFocusOptions = new XYFocusOptions();
			xyFocusOptions.SearchRoot = cDOSearchRoot;
			xyFocusOptions.ConsiderEngagement = false;
			xyFocusOptions.IgnoreClipping = ignoreClipping == true;
			xyFocusOptions.IgnoreCone = ignoreCone == true;

			candidate = FindNextFocus(focusNavigationDirection, xyFocusOptions);
			return candidate;
		}

		private static DependencyObject? FindNextElementImpl(FocusNavigationDirection focusNavigationDirection)
		{
			if (InIslandsMode())
			{
				// Return error if FindNextElement is called without focus navigation option in islands/desktop
				if (typeof(FocusManager).Log().IsEnabled(LogLevel.Error))
				{
					typeof(FocusManager).Log().LogError("FindNextElement override with FindNextElementOptions must be used in WinUI Desktop apps.");
				}
				return null;
			}

			var xyFocusOptions = new XYFocusOptions
			{
				UpdateManifold = false
			};

			var candidate = FindNextFocus(focusNavigationDirection, xyFocusOptions);
			return candidate as DependencyObject;
		}

		private static DependencyObject? FindNextElementWithOptionsImpl(
			FocusNavigationDirection focusNavigationDirection,
			FindNextElementOptions pFocusNavigationOverride)
		{
			var options = pFocusNavigationOverride;

			var xyFocusOptions = new XYFocusOptions
			{
				UpdateManifold = false
			};

			var searchRoot = options.SearchRoot;
			var exclusionRect = options.ExclusionRect;
			var hintRect = options.HintRect;
			var navigationStrategyOverride = options.XYFocusNavigationStrategyOverride;
			var ignoreOcclusivity = options.IgnoreOcclusivity;

			ContentRoot? contentRoot = null;

			if (searchRoot != null)
			{
				contentRoot = VisualTree.GetContentRootForElement(searchRoot);
			}
			else if (InIslandsMode())
			{
				// Return error if searchRootAsDO is not valid in islands/desktop mode
				throw new InvalidOperationException("Search root is not a dependency object.");
			}
			else
			{
				contentRoot = DXamlCore.Current.GetHandle().ContentRootCoordinator.CoreWindowContentRoot;
			}

			// If we are being passed in the public root visual of the XamlRoot as the SearchRoot, then override the SearchRoot to be the RootVisual.
			// This will enable searching through both the public root visual and the popup root. We will also allow Next/Prev.
			var shouldOverrideSearchRoot =
				contentRoot != null &&
				GetAppVisibleXamlRootContent(contentRoot) == searchRoot;

			if (shouldOverrideSearchRoot)
			{
				searchRoot = contentRoot!.VisualTree.RootElement;
			}
			else
			{
				if (focusNavigationDirection != FocusNavigationDirection.Up &&
					focusNavigationDirection != FocusNavigationDirection.Down &&
					focusNavigationDirection != FocusNavigationDirection.Left &&
					focusNavigationDirection != FocusNavigationDirection.Right)
				{
					throw new ArgumentOutOfRangeException(
						"Focus navigation directions Next, Previous, and None " +
						"are not supported when using FindNextElementOptions");
				}
			}

			if (searchRoot != null)
			{
				xyFocusOptions.SearchRoot = searchRoot;
			}

			if (!exclusionRect.IsUniform)
			{
				xyFocusOptions.ExclusionRect = exclusionRect;
			}

			if (!hintRect.IsUniform)
			{
				xyFocusOptions.FocusHintRectangle = hintRect;
			}

			xyFocusOptions.NavigationStrategyOverride = navigationStrategyOverride;
			xyFocusOptions.IgnoreOcclusivity = ignoreOcclusivity;

			var candidate = FindNextFocus(focusNavigationDirection, xyFocusOptions);
			return candidate as DependencyObject;
		}

		private static object? GetFocusedElementWithRootImpl(XamlRoot xamlRoot)
		{
			if (xamlRoot is null)
			{
				throw new ArgumentNullException(nameof(xamlRoot));
			}

			var focusManager = xamlRoot.VisualTree.ContentRoot.FocusManager;
			var dependencyObject = focusManager.FocusedElement;

			return dependencyObject;
		}
	}
}
