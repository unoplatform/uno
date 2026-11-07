// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference: dxaml\test\managed\enterprise\Moco\ListViewBaseTests.cs, commit bac7a9c33ff
//
// Input and tree-walking helpers used by ListViewBaseTests. WinUI gets them from its managed test
// infrastructure (TestServices.InputHelper, TestServicesExtensions, FrameworkElement.FindNameInSubtree).

#nullable disable

using System;
using System.Threading.Tasks;
using MUXControlsTestApp.Utilities;
using Microsoft.UI.Xaml.Media;
using Private.Infrastructure;
using Uno.UI.DevTools.Input;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using UIExecutor = MUXControlsTestApp.Utilities.RunOnUIThread;

namespace Microsoft.UI.Xaml.Tests.Enterprise;

internal static class TestServicesExtensions
{
	// WinUI brings the test window to the foreground after a drag may have stolen focus.
	// Nothing to do for Uno: the test window is always the one receiving the injected input.
	public static void EnsureForegroundWindow()
	{
	}
}

internal static class ListViewBaseTestsExtensions
{
	public static DependencyObject FindNameInSubtree(this DependencyObject element, string name)
	{
		if (element is null)
		{
			return null;
		}

		if (element is FrameworkElement fe && fe.Name == name)
		{
			return element;
		}

		int count = VisualTreeHelper.GetChildrenCount(element);
		for (int i = 0; i < count; i++)
		{
			var result = FindNameInSubtree(VisualTreeHelper.GetChild(element, i), name);
			if (result is not null)
			{
				return result;
			}
		}

		return null;
	}
}

internal static class ListViewBaseTestsInput
{
	private const uint Steps = 10;

	public static Task End() => TestServices.KeyboardHelper.PressKeySequence("$d$_end#$u$_end");

	public static Task Home() => TestServices.KeyboardHelper.PressKeySequence("$d$_home#$u$_home");

	// Mouse press at the center of the element, move by (relX, relY) and release.
	public static async Task DragFromCenter(FrameworkElement element, int relX, int relY, double velocityFactor)
	{
		EnsureInputInjectorSupported();

		Mouse mouse = null;
		Point center = default;
		UIExecutor.Execute(() =>
		{
			mouse = InputInjector.TryCreate()?.GetMouse() ?? throw new InvalidOperationException("Failed to create mouse");
			center = GetCenter(element);
			mouse.Press(center);
		});

		await MoveInSteps(center, relX, relY, velocityFactor, p => mouse.MoveTo(p, 1, 0));

		UIExecutor.Execute(() => mouse.Release());
	}

	// Touch press at the center of the element, hold for holdTime ms, move by (relX, relY) and release.
	public static async Task PressHoldAndPanFromCenter(FrameworkElement element, int relX, int relY, double velocityFactor, uint holdTime)
	{
		EnsureInputInjectorSupported();

		Finger finger = null;
		Point center = default;
		UIExecutor.Execute(() =>
		{
			finger = InputInjector.TryCreate()?.GetFinger() ?? throw new InvalidOperationException("Failed to create finger");
			center = GetCenter(element);
			finger.Press(center);
		});

		await Task.Delay((int)holdTime);

		await MoveInSteps(center, relX, relY, velocityFactor, p => finger.MoveTo(p, 1, 0));

		UIExecutor.Execute(() =>
		{
			finger.Release();
			finger.Dispose();
		});
	}

	// Pen press at the center of the element, move by (relX, relY) and release.
	public static async Task PenStrokeFromCenter(FrameworkElement element, int relX, int relY, double velocityFactor)
	{
		EnsureInputInjectorSupported();

#if HAS_UNO
		Pen pen = null;
		Point center = default;
		UIExecutor.Execute(() =>
		{
			pen = InputInjector.TryCreate()?.GetPen() ?? throw new InvalidOperationException("Failed to create pen");
			center = GetCenter(element);
			pen.Press(center);
		});

		await MoveInSteps(center, relX, relY, velocityFactor, p => pen.MoveTo(p, 1, 0));

		UIExecutor.Execute(() =>
		{
			pen.Release();
			pen.Dispose();
		});
#else
		await Task.CompletedTask;
		Assert.Inconclusive("Pen injection is only supported in Uno Platform targets.");
#endif
	}

	// Moves the mouse (with the given button already pressed) from one point to the other.
	public static async Task MouseDrag(Point from, Point to, MouseButton button)
	{
		EnsureInputInjectorSupported();

		Mouse mouse = null;
		UIExecutor.Execute(() =>
		{
			mouse = InputInjector.TryCreate()?.GetMouse() ?? throw new InvalidOperationException("Failed to create mouse");
			mouse.MoveTo(from);
		});

		await MoveInSteps(from, to.X - from.X, to.Y - from.Y, 1.0, p => mouse.MoveTo(p, 1, 0));
	}

	private static async Task MoveInSteps(Point start, double relX, double relY, double velocityFactor, Action<Point> moveTo)
	{
		// velocityFactor 1.0 = 1px/ms, as in WinUI's test infra.
		var durationInMilliseconds = Math.Max(1, Math.Sqrt(relX * relX + relY * relY) / velocityFactor);
		var stepDelay = (int)Math.Max(1, durationInMilliseconds / Steps);

		for (int i = 1; i <= Steps; i++)
		{
			var position = new Point(start.X + relX * i / Steps, start.Y + relY * i / Steps);
			UIExecutor.Execute(() => moveTo(position));
			await Task.Delay(stepDelay);
		}
	}

	private static Point GetCenter(FrameworkElement element)
	{
		var topLeft = element.TransformToVisual(TestServices.WindowHelper.XamlRoot.Content).TransformPoint(new Point(0, 0));
		return new Point(topLeft.X + element.RenderSize.Width / 2, topLeft.Y + element.RenderSize.Height / 2);
	}

	private static void EnsureInputInjectorSupported()
	{
#if !WINAPPSDK && !HAS_INPUT_INJECTOR
		Assert.Inconclusive("InputInjector is not supported on this platform.");
#endif
	}
}
