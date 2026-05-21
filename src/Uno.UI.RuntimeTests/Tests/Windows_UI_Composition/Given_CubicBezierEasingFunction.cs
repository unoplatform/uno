using System;
using System.Numerics;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
[RunsOnUIThread]
public class Given_CubicBezierEasingFunction
{
	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_AggressiveEaseOut_Then_MidpointNearOne()
	{
		// Reproduces the curve from https://github.com/unoplatform/uno/issues -- an
		// aggressive ease-out where the X coordinates of both control points are
		// small, so the animation should reach almost its full Y value very early.
		var compositor = ElementCompositionPreview.GetElementVisual(new Microsoft.UI.Xaml.Controls.Border()).Compositor;
		var easing = compositor.CreateCubicBezierEasingFunction(
			new Vector2(0.05f, 0.95f),
			new Vector2(0.10f, 1.00f));

		// At t = 0.5 the previous buggy implementation returned ~0.856 (it treated
		// t as the Bezier parameter). The correct value evaluated with B_x(s) = t
		// is ~0.981 — past 95%, as expected for an aggressive ease-out.
		var midpoint = InvokeEase(easing, 0.5f);
		Assert.IsTrue(midpoint > 0.95f, $"Expected midpoint > 0.95, was {midpoint}");

		// Endpoints stay anchored.
		Assert.AreEqual(0f, InvokeEase(easing, 0f));
		Assert.AreEqual(1f, InvokeEase(easing, 1f));
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_LinearControlPoints_Then_OutputEqualsInput()
	{
		// Control points on the line of identity should produce y == x.
		var compositor = ElementCompositionPreview.GetElementVisual(new Microsoft.UI.Xaml.Controls.Border()).Compositor;
		var easing = compositor.CreateCubicBezierEasingFunction(
			new Vector2(0.25f, 0.25f),
			new Vector2(0.75f, 0.75f));

		for (var i = 0; i <= 10; i++)
		{
			var t = i / 10f;
			Assert.AreEqual(t, InvokeEase(easing, t), 1e-3f);
		}
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
	public void When_CssEaseInOut_Then_MatchesReference()
	{
		// The classic CSS "ease-in-out" curve: (0.42, 0) (0.58, 1).
		// Reference values produced by the standard WebKit UnitBezier solver.
		var compositor = ElementCompositionPreview.GetElementVisual(new Microsoft.UI.Xaml.Controls.Border()).Compositor;
		var easing = compositor.CreateCubicBezierEasingFunction(
			new Vector2(0.42f, 0f),
			new Vector2(0.58f, 1f));

		Assert.AreEqual(0f, InvokeEase(easing, 0f), 1e-3f);
		Assert.AreEqual(0.5f, InvokeEase(easing, 0.5f), 1e-3f);
		Assert.AreEqual(1f, InvokeEase(easing, 1f), 1e-3f);

		// Symmetry across the midpoint: ease(t) + ease(1-t) ≈ 1.
		for (var i = 1; i < 10; i++)
		{
			var t = i / 10f;
			var sum = InvokeEase(easing, t) + InvokeEase(easing, 1f - t);
			Assert.AreEqual(1f, sum, 1e-3f);
		}
	}

	private static float InvokeEase(CubicBezierEasingFunction easing, float t)
		=> easing.Ease(t);
}
