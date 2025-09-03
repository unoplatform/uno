using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using FluentAssertions;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

public partial class Given_UIElement
{
	[TestMethod]
	[RunsOnUIThread]
#if !HAS_INPUT_INJECTOR
	[Ignore("InputInjector is not supported on this platform.")]
#endif
	public async Task When_ManipulationEvents_Then_PositionIsRelative()
	{
		var sut = new Border
		{
			Width = 200,
			Height = 200,
			ManipulationMode = ManipulationModes.TranslateX | ManipulationModes.TranslateY,
			Background = new SolidColorBrush(Colors.DeepPink)
		};

		Point started = default, delta = default, completed = default;
		sut.ManipulationStarted += (snd, args) => started = args.Position;
		sut.ManipulationDelta += (snd, args) => delta = args.Position;
		sut.ManipulationCompleted += (snd, args) => completed = args.Position;

		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		var bounds = await UITestHelper.Load(sut);

		finger.Drag(bounds.GetLocation().Offset(10, 10), bounds.GetLocation().Offset(-100, 10), steps: 2);

		started.X.Should().BeInRange(-100 - 1, 10 + 1);
		started.Y.Should().BeApproximately(10, precision: 1);

		delta.X.Should().BeInRange(-100 - 1, 10 + 1);
		delta.Y.Should().BeApproximately(10, precision: 1);

		completed.X.Should().BeApproximately(-100, precision: 1);
		completed.Y.Should().BeApproximately(10, precision: 1);
	}
}
