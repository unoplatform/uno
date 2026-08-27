#if HAS_INPUT_INJECTOR && !WINAPPSDK
using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit;
using Uno.UI.Toolkit.DevTools.Input;
using Windows.UI;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Toolkit;

[TestClass]
[RunsOnUIThread]
public class Given_ManipulationExtensions
{
	[TestMethod]
	public async Task When_PressStopsInertia_Without_OptIn_Then_TapIsRaised()
	{
		// Pins the default behavior, which is the WinUI one: gesture recognition is per element and unrelated to
		// the inertia of an ancestor, so the very press which stops the momentum still activates the pointed item.
		var taps = await PressWhileCoasting(isTapToStopInertiaEnabled: false);

		taps.Cell.Should().Be(1, because: "without the opt-in the press which stops the inertia still taps");
	}

	[TestMethod]
	public async Task When_PressStopsInertia_With_OptIn_Then_NoTapIsRaisedOnThePath()
	{
		var taps = await PressWhileCoasting(isTapToStopInertiaEnabled: true);

		taps.Cell.Should().Be(0, because: "the press which stopped the inertia must not tap the original source");
		taps.Row.Should().Be(0, because: "the press which stopped the inertia must not tap the intermediate element");
		taps.Scroller.Should().Be(0, because: "the press which stopped the inertia must not tap the coasting element");
		taps.Ancestor.Should().Be(0, because: "the press which stopped the inertia must not tap the ancestor");
	}

	[TestMethod]
	public async Task When_TapAtRest_With_OptIn_Then_TapIsRaisedOnThePath()
	{
		var taps = BuildTree(isTapToStopInertiaEnabled: true, out var ancestor, out _, out _);

		var bounds = await UITestHelper.Load(ancestor);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Tap(bounds.GetCenter());

		taps.Cell.Should().Be(1, because: "the opt-in must not affect a tap while the content is at rest");
		taps.Row.Should().Be(1, because: "the tap of the original source must bubble to the intermediate element");
		taps.Scroller.Should().Be(1, because: "the tap of the original source must bubble to the coasting element");
		taps.Ancestor.Should().Be(1, because: "the tap of the original source must bubble to the ancestor");
	}

	private static async Task<TapCounts> PressWhileCoasting(bool isTapToStopInertiaEnabled)
	{
		var taps = BuildTree(isTapToStopInertiaEnabled, out var ancestor, out var scroller, out _);

		var inertiaStarted = false;
		scroller.ManipulationInertiaStarting += (_, _) => inertiaStarted = true;

		var bounds = await UITestHelper.Load(ancestor);

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");

		// Flick fast enough to start the inertia. Do NOT await WaitForIdle: the inertia must still be running
		// when the next press is injected.
		using var flickFinger = injector.GetFinger();
		flickFinger.Drag(from: bounds.GetCenter(), to: bounds.GetCenter().Offset(y: -200), steps: 1, stepOffsetInMilliseconds: 50);

		inertiaStarted.Should().BeTrue(because: "the fast flick should have started the manipulation inertia");

		using var tapFinger = injector.GetFinger(id: 43);
		tapFinger.Tap(bounds.GetCenter());

		return taps;
	}

	/// <summary>
	/// Builds the shape of a CommunityToolkit DataGrid: a panning element (the rows presenter) whose descendants
	/// (the row and its cell) act on tap.
	/// </summary>
	private static TapCounts BuildTree(bool isTapToStopInertiaEnabled, out Border ancestor, out Border scroller, out Border cell)
	{
		Border row;
		ancestor = new Border
		{
			Width = 200,
			Height = 600,
			Background = new SolidColorBrush(Colors.DarkSlateGray),
			Child = (scroller = new Border
			{
				Background = new SolidColorBrush(Colors.DeepSkyBlue),
				ManipulationMode = ManipulationModes.TranslateY | ManipulationModes.TranslateInertia,
				Child = (row = new Border
				{
					Background = new SolidColorBrush(Colors.BlueViolet),
					Padding = new Thickness(10),
					Child = (cell = new Border
					{
						Background = new SolidColorBrush(Colors.DeepPink),
					}),
				}),
			}),
		};

		scroller.SetIsTapToStopInertiaEnabled(isTapToStopInertiaEnabled);

		var taps = new TapCounts();
		ancestor.Tapped += (_, _) => taps.Ancestor++;
		scroller.Tapped += (_, _) => taps.Scroller++;
		row.Tapped += (_, _) => taps.Row++;
		cell.Tapped += (_, _) => taps.Cell++;

		return taps;
	}

	private sealed class TapCounts
	{
		public int Ancestor;
		public int Scroller;
		public int Row;
		public int Cell;
	}
}
#endif
