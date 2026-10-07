using System;
using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Uno.UI.DevTools.Input;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_UIElement_Manipulation
{
	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_Container_Set_On_Starting_Then_Used_By_Following_Events()
	{
		var (sut, container) = CreateSetup();
		sut.ManipulationStarting += (_, e) => e.Container = container;

		UIElement startedContainer = null, deltaContainer = null, completedContainer = null;
		Point completedPosition = default;
		sut.ManipulationStarted += (_, e) => startedContainer = e.Container;
		sut.ManipulationDelta += (_, e) => deltaContainer = e.Container;
		sut.ManipulationCompleted += (_, e) =>
		{
			completedContainer = e.Container;
			completedPosition = e.Position;
		};

		try
		{
			await UITestHelper.Load(container);
			var sutOrigin = sut.TransformToVisual(container).TransformPoint(default);

			Drag(sut, from: new Point(10, 10), to: new Point(60, 10));

			Assert.AreSame(container, startedContainer);
			Assert.AreSame(container, deltaContainer);
			Assert.AreSame(container, completedContainer);
			Assert.AreEqual(sutOrigin.X + 60, completedPosition.X, 1);
			Assert.AreEqual(sutOrigin.Y + 10, completedPosition.Y, 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_Container_Cleared_On_Starting_Then_Position_Is_Global()
	{
		var (sut, container) = CreateSetup();
		sut.ManipulationStarting += (_, e) => e.Container = null;

		var completedContainer = (UIElement)sut;
		Point completedPosition = default;
		sut.ManipulationCompleted += (_, e) =>
		{
			completedContainer = e.Container;
			completedPosition = e.Position;
		};

		try
		{
			await UITestHelper.Load(container);
			var expected = sut.TransformToVisual(null).TransformPoint(new Point(60, 10));

			Drag(sut, from: new Point(10, 10), to: new Point(60, 10));

			Assert.IsNull(completedContainer);
			Assert.AreEqual(expected.X, completedPosition.X, 1);
			Assert.AreEqual(expected.Y, completedPosition.Y, 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_Element_Moves_With_Drag_Then_Position_Stays_In_Container_Frame()
	{
		// Mirrors the ResizeGripper: the manipulated element travels with the drag, the container stays put.
		var (sut, container) = CreateSetup();
		var translate = new TranslateTransform();
		sut.RenderTransform = translate;
		sut.ManipulationStarting += (_, e) => e.Container = container;

		Point completedPosition = default;
		var completedTranslation = 0.0;
		sut.ManipulationDelta += (_, e) => translate.X = e.Cumulative.Translation.X;
		sut.ManipulationCompleted += (_, e) =>
		{
			completedPosition = e.Position;
			completedTranslation = e.Cumulative.Translation.X;
		};

		try
		{
			await UITestHelper.Load(container);
			var sutOrigin = sut.TransformToVisual(container).TransformPoint(default);

			Drag(sut, from: new Point(10, 10), to: new Point(60, 10));

			Assert.AreEqual(50, completedTranslation, 1);
			Assert.AreEqual(sutOrigin.X + 60, completedPosition.X, 1);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static (Border sut, Grid container) CreateSetup()
	{
		var sut = new Border
		{
			Width = 100,
			Height = 100,
			ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY,
			Background = new SolidColorBrush(Colors.DeepPink),
		};
		var container = new Grid
		{
			Width = 300,
			Height = 300,
			Background = new SolidColorBrush(Colors.Green),
			Children = { sut },
		};

		return (sut, container);
	}

	private static void Drag(UIElement sut, Point from, Point to)
	{
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		var toHost = sut.TransformToVisual(null);
		finger.Drag(toHost.TransformPoint(from), toHost.TransformPoint(to), steps: 2);
	}
}
