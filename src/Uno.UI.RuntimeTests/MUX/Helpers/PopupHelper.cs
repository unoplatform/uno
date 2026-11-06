// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference PopupHelper.h, commit bac7a9c33

using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.MUX.Helpers;

internal static class PopupHelper
{
	public static async Task OpenPopup(Popup popup)
	{
		var openedEvent = new Event();
		var openedRegistration = CreateSafeEventRegistration<Popup, EventHandler<object>>(nameof(Popup.Opened));

		await RunOnUIThread(() =>
		{
			openedRegistration.Attach(popup, (s, e) => openedEvent.Set());

			popup.IsOpen = true;
		});

		await openedEvent.WaitForDefault();
		await TestServices.WindowHelper.WaitForIdle();
	}

	// Upstream: TestServices::Utilities->IsDesktop. Windowed popups are only a thing on the desktop Skia heads.
	public static bool AreWindowedPopupsEnabled()
		=> OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

	public static async Task WaitForOpenPopup(UIElement element)
	{
		const int retries = 20;

		for (var i = 0; i < retries; i++)
		{
			var popupWasFound = false;

			await RunOnUIThread(() =>
			{
				var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(element.XamlRoot);
				if (popups.Count > 0)
				{
					popupWasFound = true;
				}
			});

			if (popupWasFound)
			{
				break;
			}
			else
			{
				if (i < retries - 1)
				{
					LOG_OUTPUT("TimePicker popup not yet found. Retrying...");
					await Task.Delay(100);
				}
				else
				{
					Assert.Fail("TimePicker popup never opened.");
				}
			}
		}
	}

	// Upstream: TestServices::WindowHelper->Popup_SetWindowed(popup), an internal test command that forces the
	// popup to be windowed. Uno gets there through the public ShouldConstrainToRootBounds opt-out, which has to
	// be set before the popup opens.
	public static void SetWindowed(Popup popup)
		=> popup.ShouldConstrainToRootBounds = false;

	// True when the popup is
	// hosted in its own OS window (WinUI: CPopup::IsWindowed()).
	public static bool IsWindowed(Popup popup)
#if HAS_UNO
		=> popup.IsWindowed;
#else
		=> throw new NotSupportedException("IsWindowed is an Uno test hook.");
#endif

	// The rect the popup's window was last moved
	// and resized to, in physical pixels (WinUI: CPopup::m_windowedPopupMoveAndResizeRect). It includes the
	// drop shadow gutters. The origin is the top-left of the XamlRoot content (not the screen), so
	// the expectations do not depend on where the host window sits.
	public static Rect GetWindowedPopupMoveAndResizeRect(Popup popup)
#if HAS_UNO
		=> popup.GetWindowedPopupMoveAndResizeRect();
#else
		=> throw new NotSupportedException("GetWindowedPopupMoveAndResizeRect is an Uno test hook.");
#endif

	// Verifies the windowed popup's window rect against expectations expressed in DIPs at 100% scale,
	// relative to the XamlRoot content origin. Allows 1 physical pixel of rounding slack, and polls until the
	// popup's window gets there since the move is driven by frames.
	// The masters' HwndTop sits on a window whose client area starts 23 px down. That offset follows from
	// CThemeShadow::GetInsetsForWindowedPopup (ThemeShadow.cpp:190): 10 on the left, 2 on top, and a popup at the
	// origin has HwndLeft = -10 and HwndTop = 21. So an expected Y here is HwndTop - 23.
	public static async Task VerifyWindowedPopupRect(Popup popup, double x, double y, double width, double height)
	{
		var scale = popup.XamlRoot.RasterizationScale;

		bool IsExpected(Rect r)
			=> Math.Abs(r.X - x * scale) <= 1
				&& Math.Abs(r.Y - y * scale) <= 1
				&& Math.Abs(r.Width - width * scale) <= 1
				&& Math.Abs(r.Height - height * scale) <= 1;

		var rect = GetWindowedPopupMoveAndResizeRect(popup);
		var stopwatch = System.Diagnostics.Stopwatch.StartNew();
		while (!IsExpected(rect) && stopwatch.ElapsedMilliseconds < 2000)
		{
			await TestServices.WindowHelper.WaitForIdle();
			await Task.Delay(16);
			rect = GetWindowedPopupMoveAndResizeRect(popup);
		}

		LOG_OUTPUT($"Windowed popup rect (physical): (X, Y, Width, Height) = ({rect.X:0}, {rect.Y:0}, {rect.Width:0}, {rect.Height:0}), expected (DIPs at 100%): ({x:0}, {y:0}, {width:0}, {height:0}), scale {scale}");

		VERIFY_ARE_VERY_CLOSE(rect.X, x * scale, 1, "HWND left");
		VERIFY_ARE_VERY_CLOSE(rect.Y, y * scale, 1, "HWND top");
		VERIFY_ARE_VERY_CLOSE(rect.Width, width * scale, 1, "HWND width");
		VERIFY_ARE_VERY_CLOSE(rect.Height, height * scale, 1, "HWND height");
	}

	// Upstream: InputHelper::ClickMouseButton(button, element, offsetFromCenterX, offsetFromCenterY).
	// Works from the element's bounds so it is correct under RTL as well.
	public static async Task ClickMouseButton(MouseButton button, FrameworkElement element, double offsetFromCenterX = 0, double offsetFromCenterY = 0)
	{
		var bounds = await ControlHelper.GetBounds(element);
		var position = new Point(
			bounds.X + bounds.Width / 2 + offsetFromCenterX,
			bounds.Y + bounds.Height / 2 + offsetFromCenterY);

		TestServices.InputHelper.ClickMouseButton(button, position);
	}
}
