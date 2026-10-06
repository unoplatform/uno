using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.Foundation;

using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ScrollViewerTests;

[TestClass]
[RunsOnUIThread]
public class Given_ScrollViewer_AnchoringAllocations
{
	[TestMethod]
	[RequiresFullWindow]
	public async Task When_ItemsRepeater_ScrollStep_Does_Not_Allocate_Transforms()
	{
		var repeater = new ItemsRepeater
		{
			ItemsSource = Enumerable.Range(0, 1000).Select(i => $"Item {i}").ToList(),
			Layout = new StackLayout(),
			ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(
				"<DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'><Border Height='40'><TextBlock Text='{Binding}'/></Border></DataTemplate>"),
		};
		var sv = new ScrollViewer { Width = 300, Height = 600, Content = repeater };

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);
		await WindowHelper.WaitForIdle();

		void Step(double offset)
		{
			sv.ChangeView(null, offset, null, disableAnimation: true);
			sv.UpdateLayout();
		}

		// Warm-up (JIT, template realization, pools)
		for (var i = 1; i <= 10; i++)
		{
			Step(i * 400);
		}

		const int steps = 20;
		GC.Collect();
		var before = GC.GetAllocatedBytesForCurrentThread();
		for (var i = 11; i < 11 + steps; i++)
		{
			Step(i * 400);
		}
		var perStep = (GC.GetAllocatedBytesForCurrentThread() - before) / steps;

		Console.WriteLine($"ScrollStep allocated {perStep} bytes per step");
		Assert.IsTrue(perStep < 150_000, $"A scroll step allocated {perStep} bytes (expected < 150000)");
	}

	[TestMethod]
	[RequiresFullWindow]
	public async Task When_Descendant_Transformed_Bounds_Match_TransformToVisual()
	{
		var inner = new Border { Width = 50, Height = 30, Margin = new Thickness(7, 11, 0, 0) };
		inner.RenderTransformOrigin = new Point(0.5, 0.5);
		inner.RenderTransform = new CompositeTransform { Rotation = 30, ScaleX = 1.5, TranslateX = 9, TranslateY = 4 };
		var content = new StackPanel { Margin = new Thickness(13, 5, 0, 0), Children = { new Border { Height = 20 }, inner } };
		var sv = new ScrollViewer { Width = 300, Height = 300, Content = content };

		WindowHelper.WindowContent = sv;
		await WindowHelper.WaitForLoaded(sv);
		await WindowHelper.WaitForIdle();

		var expected = inner.TransformToVisual(content).TransformBounds(new Rect(
			content.Margin.Left, content.Margin.Top, inner.ActualWidth, inner.ActualHeight));
		var actual = ScrollViewer.GetDescendantBounds(content, inner);

		Assert.AreEqual(expected.X, actual.X, 0.01);
		Assert.AreEqual(expected.Y, actual.Y, 0.01);
		Assert.AreEqual(expected.Width, actual.Width, 0.01);
		Assert.AreEqual(expected.Height, actual.Height, 0.01);
	}
}
