// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// https://github.com/CommunityToolkit/Microsoft.Toolkit.Win32/blob/master/Microsoft.Toolkit.Wpf.UI.XamlHost/WindowsXamlHostBase.Focus.cs

using System;
using System.Collections.Generic;
using System.Windows;
using WF = Windows.Foundation;
using WUX = Windows.UI.Xaml;

//TODO: We need to make sure that when the UnoXamlHost loses focus, focus is changed in the XamlRoot as well,
//so that for active input fields the native overlay is closed and changes are committed to underlying TextBox text
//before potential data binding changes. https://github.com/unoplatform/uno/issues/8978[focus]

namespace Uno.UI.XamlHost.Skia.Wpf
{
	/// <summary>
	/// Focus and Keyboard handling for Focus integration with UWP XAML
	/// </summary>
	partial class UnoXamlHostBase
	{
		/// <summary>
		/// Dictionary that maps WPF (host framework) FocusNavigationDirection to UWP XAML XxamlSourceFocusNavigationReason
		/// </summary>
		private static readonly Dictionary<System.Windows.Input.FocusNavigationDirection, WUX.Hosting.XamlSourceFocusNavigationReason>
			MapDirectionToReason =
				new Dictionary<System.Windows.Input.FocusNavigationDirection, WUX.Hosting.XamlSourceFocusNavigationReason>
				{
					{ System.Windows.Input.FocusNavigationDirection.Next,     WUX.Hosting.XamlSourceFocusNavigationReason.First },
					{ System.Windows.Input.FocusNavigationDirection.First,    WUX.Hosting.XamlSourceFocusNavigationReason.First },
					{ System.Windows.Input.FocusNavigationDirection.Previous, WUX.Hosting.XamlSourceFocusNavigationReason.Last },
					{ System.Windows.Input.FocusNavigationDirection.Last,     WUX.Hosting.XamlSourceFocusNavigationReason.Last },
					{ System.Windows.Input.FocusNavigationDirection.Up,       WUX.Hosting.XamlSourceFocusNavigationReason.Up },
					{ System.Windows.Input.FocusNavigationDirection.Down,     WUX.Hosting.XamlSourceFocusNavigationReason.Down },
					{ System.Windows.Input.FocusNavigationDirection.Left,     WUX.Hosting.XamlSourceFocusNavigationReason.Left },
					{ System.Windows.Input.FocusNavigationDirection.Right,    WUX.Hosting.XamlSourceFocusNavigationReason.Right },
				};

		/// <summary>
		/// Dictionary that maps UWP XAML XamlSourceFocusNavigationReason to WPF (host framework) FocusNavigationDirection
		/// </summary>
		private static readonly Dictionary<WUX.Hosting.XamlSourceFocusNavigationReason, System.Windows.Input.FocusNavigationDirection>
			MapReasonToDirection =
				new Dictionary<WUX.Hosting.XamlSourceFocusNavigationReason, System.Windows.Input.FocusNavigationDirection>()
				{
					{ WUX.Hosting.XamlSourceFocusNavigationReason.First, System.Windows.Input.FocusNavigationDirection.Next },
					{ WUX.Hosting.XamlSourceFocusNavigationReason.Last,  System.Windows.Input.FocusNavigationDirection.Previous },
					{ WUX.Hosting.XamlSourceFocusNavigationReason.Up,    System.Windows.Input.FocusNavigationDirection.Up },
					{ WUX.Hosting.XamlSourceFocusNavigationReason.Down,  System.Windows.Input.FocusNavigationDirection.Down },
					{ WUX.Hosting.XamlSourceFocusNavigationReason.Left,  System.Windows.Input.FocusNavigationDirection.Left },
					{ WUX.Hosting.XamlSourceFocusNavigationReason.Right, System.Windows.Input.FocusNavigationDirection.Right },
				};

		/// <summary>
		/// Last Focus Request GUID to uniquely identify Focus operations, primarily used with error callbacks
		/// </summary>
		private Guid _lastFocusRequest = Guid.Empty;

		/// <summary>
		/// Override for OnGotFocus that passes NavigateFocus on to the DesktopXamlSource instance
		/// </summary>
		/// <param name="e">RoutedEventArgs</param>
		protected override void OnGotFocus(RoutedEventArgs e)
		{
			base.OnGotFocus(e);

			if (!_xamlSource.HasFocus)
			{
				_xamlSource.NavigateFocus(
					new WUX.Hosting.XamlSourceFocusNavigationRequest(
						WUX.Hosting.XamlSourceFocusNavigationReason.Programmatic));
			}
		}

		///// <summary>
		///// Process Tab from host framework
		///// </summary>
		///// <param name="request"><see cref="System.Windows.Input.TraversalRequest"/> that contains requested navigation direction</param>
		///// <returns>Did handle tab</returns>
		//protected override bool TabIntoCore(System.Windows.Input.TraversalRequest request)
		//{
		//    if (_xamlSource.HasFocus && !_onTakeFocusRequested)
		//    {
		//        return false; // If we have focus already, then we dont need to NavigateFocus
		//    }

		//    // Bug 17544829: Focus is wrong if the previous element is in a different FocusScope than the UnoXamlHost element.
		//    var focusedElement = System.Windows.Input.FocusManager.GetFocusedElement(
		//        System.Windows.Input.FocusManager.GetFocusScope(this)) as FrameworkElement;

		//    var origin = BoundsRelativeTo(focusedElement, this);
		//    var reason = MapDirectionToReason[request.FocusNavigationDirection];
		//    if (_lastFocusRequest == Guid.Empty)
		//    {
		//        _lastFocusRequest = Guid.NewGuid();
		//    }

		//    var sourceFocusNavigationRequest = new WUX.Hosting.XamlSourceFocusNavigationRequest(reason, origin, _lastFocusRequest);
		//    try
		//    {
		//        var result = _xamlSource.NavigateFocus(sourceFocusNavigationRequest);

		//        // Returning true indicates that focus moved.  This will cause the HwndHost to
		//        // move focus to the source’s hwnd (call SetFocus Win32 API)
		//        return result.WasFocusMoved;
		//    }
		//    finally
		//    {
		//        _lastFocusRequest = Guid.Empty;
		//    }
		//}

#if false
		/// <summary>
		/// Transform bounds relative to FrameworkElement
		/// </summary>
		/// <param name="sibling1">base rectangle</param>
		/// <param name="sibling2">second of pair to transform</param>
		/// <returns>result of transformed rectangle</returns>
		private static WF.Rect BoundsRelativeTo(FrameworkElement sibling1, System.Windows.Media.Visual sibling2)
		{
			WF.Rect origin = default(WF.Rect);

			if (sibling1 != null)
			{
				// TransformToVisual can throw an exception if two elements don't have a common ancestor
				try
				{
					var transform = sibling1.TransformToVisual(sibling2);
					var systemWindowsRect = transform.TransformBounds(
						new Rect(0, 0, sibling1.ActualWidth, sibling1.ActualHeight));
					origin.X = systemWindowsRect.X;
					origin.Y = systemWindowsRect.Y;
					origin.Width = systemWindowsRect.Width;
					origin.Height = systemWindowsRect.Height;
				}
				catch (System.InvalidOperationException)
				{
				}
			}

			return origin;
		}
#endif

		//private bool _onTakeFocusRequested;

		/// <summary>
		/// Handles the <see cref="WUX.Hosting.DesktopWindowXamlSource.TakeFocusRequested" /> event.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="WUX.Hosting.DesktopWindowXamlSourceTakeFocusRequestedEventArgs"/> instance containing the event data.</param>
		private void OnTakeFocusRequested(object sender, WUX.Hosting.DesktopWindowXamlSourceTakeFocusRequestedEventArgs e)
		{
			if (_lastFocusRequest == e.Request.CorrelationId)
			{
				// If we've arrived at this point, then focus is being move back to us
				// therefore, we should complete the operation to avoid an infinite recursion
				// by "Restoring" the focus back to us under a new correctationId
				var newRequest = new WUX.Hosting.XamlSourceFocusNavigationRequest(
					WUX.Hosting.XamlSourceFocusNavigationReason.Restore);
				_xamlSource.NavigateFocus(newRequest);
			}
			else
			{
				//_onTakeFocusRequested = true;
				//try
				{
					// Last focus request is not initiated by us, so continue
					_lastFocusRequest = e.Request.CorrelationId;
					var direction = MapReasonToDirection[e.Request.Reason];
					var request = new System.Windows.Input.TraversalRequest(direction);
					MoveFocus(request);
				}
				//finally
				//{
				//    _onTakeFocusRequested = false;
				//}
			}
		}

#if false
		private void OnThreadFilterMessage(ref System.Windows.Interop.MSG msg, ref bool handled)
		{
			if (handled)
			{
				return;
			}

			//var desktopWindowXamlSourceNative = _xamlSource.GetInterop<IDesktopWindowXamlSourceNative2>();
			//if (desktopWindowXamlSourceNative != null)
			//{
			//    handled = desktopWindowXamlSourceNative.PreTranslateMessage(msg);
			//}
		}
#endif

		//protected override bool HasFocusWithinCore()
		//{
		//    return _xamlSource.HasFocus;
		//}
	}
}
