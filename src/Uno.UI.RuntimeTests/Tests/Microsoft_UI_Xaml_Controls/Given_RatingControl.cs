using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
public class Given_RatingControl
{
	[TestMethod]
	[RunsOnUIThread]
	public async Task When_Loaded_Then_Unloaded_Tap()
	{
		// Create RatingControl
		var ratingControl = new RatingControl();

		TestServices.WindowHelper.WindowContent = ratingControl;
		bool unloaded = false;
		await TestServices.WindowHelper.WaitForLoaded(ratingControl);
		ratingControl.Unloaded += (s, e) => unloaded = true;

		// Unload RatingControl
		TestServices.WindowHelper.WindowContent = null;
		await TestServices.WindowHelper.WaitFor(() => unloaded);

		// Re-add RatingControl
		TestServices.WindowHelper.WindowContent = ratingControl;
		await TestServices.WindowHelper.WaitForLoaded(ratingControl);

		// Tap RatingControl
		ratingControl.Value = 1;
		var tapTarget = ratingControl.TransformToVisual(null).TransformPoint(new Point(ratingControl.ActualWidth * 0.9, ratingControl.ActualHeight / 2));
		var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var finger = injector.GetFinger();

		finger.Press(tapTarget);
		finger.Release();

		Assert.AreEqual(5, ratingControl.Value);
	}
}
