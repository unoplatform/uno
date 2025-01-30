// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FocusRectManager.h, FocusRectManager.cpp

#nullable enable


using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Controls;
using Uno.UI.Xaml.Core;
using Uno.UI.Xaml.Core.Rendering;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;

namespace Uno.UI.Xaml.Input
{
	/// <summary>
	/// This handles "Focus Visuals" on WinUI.	
	/// </summary>
	/// <remarks>
	/// Our implementation is fairly limited currently, mostly stubs.
	/// </remarks>
	internal class FocusRectManager
	{
		private SystemFocusVisual? _focusVisual;

		[NotImplemented]
		internal void OnFocusedElementKeyPressed()
		{
			//TODO Uno: Implement state handling
		}

		[NotImplemented]
		internal void OnFocusedElementKeyReleased()
		{
			//TODO Uno: Implement state handling
		}

		internal static bool AreHighVisibilityFocusRectsEnabled() =>
			Application.Current.FocusVisualKind != FocusVisualKind.DottedLine;

		internal static bool AreRevealFocusRectsEnabled() =>
			Application.Current.FocusVisualKind == FocusVisualKind.Reveal;

		internal void RenderFocusRectForElement(UIElement element, IContentRenderer? contentRenderer)
		{
			// Legacy mode only
			if (AreHighVisibilityFocusRectsEnabled())
			{
				return;
			}

			//Focusable focusTarget(element);
			//FocusRectangleOptions options;
			//if (SetLegacyRenderOptions(focusTarget, &options))
			//{
			//	options.bounds = focusTarget.GetBounds();
			//	IFCFAILFAST(contentRenderer->RenderFocusRectangle(element, options));
			//}
		}

		internal void ReleaseResources(bool isDeviceLost, bool cleanupDComp, bool clearPCData)
		{
			//TODO Uno: Implement a shorthand of the WinUI implementation, maybe removing focus visual if that is what the code does as by-product.
		}

		internal void UpdateFocusRect(
			DependencyObject? focusedElement,
			DependencyObject? focusTarget,
			FocusNavigationDirection focusNavigationDirection,
			bool cleanOnly)
		{
			// TODO Uno specific: This implementation differs significantly from WinUI.
			VisualTree? visualTree = null;
			if (focusedElement is UIElement focusedUIElement)
			{
				visualTree = focusedUIElement.XamlRoot?.VisualTree;
			}

			if (visualTree?.FocusVisualRoot is { } focusVisualLayer)
			{
				if (_focusVisual == null)
				{
					focusVisualLayer.Children.Add(_focusVisual = new SystemFocusVisual());
				}

				if (focusedElement is FrameworkElement uiElement && uiElement.FocusState == FocusState.Keyboard && uiElement.UseSystemFocusVisuals)
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"Showing focus rect for {focusedElement?.GetType().Name} and state {(focusedElement as UIElement)?.FocusState} and uses system focus visuals {(focusedElement as UIElement)?.UseSystemFocusVisuals}");
					}
					_focusVisual.FocusedElement = (focusedElement as FrameworkElement) ?? uiElement;
					_focusVisual.Visibility = Visibility.Visible;
				}
				else
				{
					if (this.Log().IsEnabled(LogLevel.Debug))
					{
						this.Log().LogDebug($"Hiding focus rect");
					}
					_focusVisual.FocusedElement = null;
					_focusVisual.Visibility = Visibility.Collapsed;
				}
			}
		}

		internal void RedrawFocusVisual()
		{
			_focusVisual?.Redraw();
		}
	}
}
