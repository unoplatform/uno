#nullable enable

#if __SKIA__

using System;
using System.Numerics;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.Interactions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ScrollView
{
	/// <summary>A precision touchpad reports wheel deltas finer than one 120-unit detent, and each must still scroll.</summary>
	[TestMethod]
	public async Task When_Wheel_Delta_Below_A_Detent_Then_Scrolls()
	{
		var (sut, bounds) = await LoadTallScrollView();

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();
		mouse.MoveTo(Center(bounds));
		mouse.Wheel(-30);

		await TestServices.WindowHelper.WaitFor(() => sut.VerticalOffset > 0, message: "a sub-detent wheel delta should scroll");
		await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);

		Assert.AreEqual(12, sut.VerticalOffset, 0.5, "a quarter detent should scroll a quarter of the 48px a detent scrolls");
	}

	/// <summary>
	/// The tracker's position is what the frame recorded in the same tick has to show: raising its change from a
	/// dispatcher continuation instead left the content a hop behind the tracker.
	/// </summary>
	[TestMethod]
	public async Task When_Wheel_Inertia_Then_Recorded_Frame_Shows_Tracker_Position()
	{
		var (sut, bounds) = await LoadTallScrollView();
		var presenter = sut.ScrollPresenter!;
		var content = presenter.Content!;
		var tracker = GetTracker(presenter);
		var target = (CompositionTarget)content.Visual.CompositionTarget!;

		var frames = 0;
		var mismatches = 0;
		float? offset = null;
		Action onFrameRendered = () =>
		{
			if (content.Visual.Properties.TryGetVector3("Translation", out var translation) != CompositionGetValueStatus.Succeeded)
			{
				return;
			}

			// Whatever constant the expression adds, it is the same in every frame.
			var current = translation.Y + tracker.Position.Y;
			offset ??= current;
			frames++;
			if (Math.Abs(current - offset.Value) > 0.01f)
			{
				mismatches++;
			}
		};

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = injector.GetMouse();
		mouse.MoveTo(Center(bounds));

		target.FrameRendered += onFrameRendered;
		try
		{
			mouse.Wheel(-120 * 3, steps: 3);
			await TestServices.WindowHelper.WaitFor(() => sut.VerticalOffset > 0, message: "the wheel should scroll");
			await UITestHelper.WaitForIdle(waitForCompositionAnimations: true);
		}
		finally
		{
			target.FrameRendered -= onFrameRendered;
		}

		Assert.IsTrue(frames >= 5, $"expected the wheel inertia to span several frames, got {frames}");
		Assert.AreEqual(0, mismatches, $"{mismatches} of {frames} recorded frames showed a position the tracker had already left");
	}

	/// <summary>A finger pressed and held on coasting content stops it, without having to move first.</summary>
	[TestMethod]
	public async Task When_Finger_Held_On_Coasting_Content_Then_Stops()
	{
		var (sut, bounds) = await LoadTallScrollView();
		var center = Center(bounds);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Press(center);
		finger.MoveTo(new Point(center.X, center.Y - 200), steps: 10, stepOffsetInMilliseconds: 5);
		finger.Release();

		await TestServices.WindowHelper.WaitFor(() => sut.VerticalOffset > 250, message: "the fling should coast past where the finger lifted");

		finger.Press(center);
		try
		{
			await TestServices.WindowHelper.WaitForIdle();
			var held = sut.VerticalOffset;

			await Task.Delay(300);

			Assert.AreEqual(held, sut.VerticalOffset, 1, "the content should not move under a finger held still");
		}
		finally
		{
			finger.Release();
		}
	}

	private static async Task<(ScrollView ScrollView, Rect Bounds)> LoadTallScrollView()
	{
		var sut = new ScrollView
		{
			Width = 300,
			Height = 300,
			Content = new Border
			{
				Height = 20000,
				Background = new LinearGradientBrush
				{
					StartPoint = new Point(0, 0),
					EndPoint = new Point(0, 1),
					GradientStops =
					{
						new GradientStop { Color = Colors.Red, Offset = 0 },
						new GradientStop { Color = Colors.Blue, Offset = 1 },
					},
				},
			},
		};

		var bounds = await UITestHelper.Load(sut);
		await TestServices.WindowHelper.WaitForIdle();

		return (sut, bounds);
	}

	private static Point Center(Rect bounds) => new(bounds.X + bounds.Width / 2, bounds.Y + bounds.Height / 2);

	private static InteractionTracker GetTracker(ScrollPresenter presenter)
		=> (InteractionTracker)typeof(ScrollPresenter)
			.GetField("m_interactionTracker", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(presenter)!;
}
#endif
