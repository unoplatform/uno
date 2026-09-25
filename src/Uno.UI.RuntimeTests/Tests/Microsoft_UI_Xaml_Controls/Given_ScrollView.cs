#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_ScrollView
{
	private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(5);

	[TestMethod]
	public async Task When_ScrollTo_Default_Options_Then_Animates_And_Completes_Once()
	{
		var (scrollView, _) = await LoadScrollView();
		var completions = TrackScrollCompleted(scrollView);
		var verticalOffsets = TrackVerticalOffsets(scrollView);

		var correlationId = scrollView.ScrollTo(0, 500);

		await WaitForCompletion(completions, correlationId);
		await WaitForFrames(5);

		Assert.AreEqual(500, scrollView.VerticalOffset, 0.5);
		Assert.AreEqual(1, completions.Count(id => id == correlationId), "ScrollCompleted must be raised exactly once.");
		Assert.IsTrue(
			verticalOffsets.Any(offset => offset > 1 && offset < 499),
			$"Expected intermediate offsets while animating, got: {string.Join(", ", verticalOffsets)}");
	}

	[TestMethod]
	public async Task When_ScrollBy_Default_Options_Then_Animates_To_Target()
	{
		var (scrollView, _) = await LoadScrollView();
		var completions = TrackScrollCompleted(scrollView);

		var firstId = scrollView.ScrollTo(0, 100, new ScrollingScrollOptions(ScrollingAnimationMode.Disabled));
		await WaitForCompletion(completions, firstId);

		var correlationId = scrollView.ScrollBy(0, 250);
		await WaitForCompletion(completions, correlationId);

		Assert.AreEqual(350, scrollView.VerticalOffset, 0.5);
		Assert.AreEqual(1, completions.Count(id => id == correlationId));
	}

	[TestMethod]
	public async Task When_ScrollTo_Interrupted_By_ScrollTo_Then_Ends_At_Second_Target()
	{
		var (scrollView, _) = await LoadScrollView();
		var completions = TrackScrollCompleted(scrollView);

		var firstId = scrollView.ScrollTo(0, 1500);
		await WaitForFrames(3);
		var secondId = scrollView.ScrollTo(0, 300);

		await WaitForCompletion(completions, secondId);
		await WaitForCompletion(completions, firstId);
		await WaitForFrames(5);

		Assert.AreEqual(300, scrollView.VerticalOffset, 0.5);
		Assert.AreEqual(1, completions.Count(id => id == firstId), "Interrupted request must complete exactly once.");
		Assert.AreEqual(1, completions.Count(id => id == secondId), "Second request must complete exactly once.");
	}

	[TestMethod]
	public async Task When_Home_End_Keys_Then_Scrolls_To_Extents()
	{
		var (scrollView, _) = await LoadScrollView();
		var completions = TrackScrollCompleted(scrollView);

		scrollView.Focus(FocusState.Keyboard);
		await WindowHelper.WaitForIdle();

		await KeyboardHelper.PressKeySequence("$d$_end#$u$_end", scrollView);
		await WaitFor(() => completions.Count == 1);
		Assert.AreEqual(scrollView.ScrollableHeight, scrollView.VerticalOffset, 0.5);

		await KeyboardHelper.PressKeySequence("$d$_home#$u$_home", scrollView);
		await WaitFor(() => completions.Count == 2);
		Assert.AreEqual(0, scrollView.VerticalOffset, 0.5);
	}

	private static async Task<(ScrollView ScrollView, FrameworkElement Content)> LoadScrollView()
	{
		var content = new Rectangle
		{
			Width = 200,
			Height = 2000,
			Fill = new SolidColorBrush(Microsoft.UI.Colors.SteelBlue),
		};

		var scrollView = new ScrollView
		{
			Width = 300,
			Height = 200,
			Content = content,
		};

		await UITestHelper.Load(scrollView);
		return (scrollView, content);
	}

	private static List<int> TrackScrollCompleted(ScrollView scrollView)
	{
		var completions = new List<int>();
		scrollView.ScrollCompleted += (_, args) => completions.Add(args.CorrelationId);
		return completions;
	}

	private static List<double> TrackVerticalOffsets(ScrollView scrollView)
	{
		var offsets = new List<double>();
		scrollView.ViewChanged += (_, _) => offsets.Add(scrollView.VerticalOffset);
		return offsets;
	}

	private static Task WaitForCompletion(List<int> completions, int correlationId)
		=> WaitFor(() => completions.Contains(correlationId));

	private static async Task WaitFor(Func<bool> condition)
	{
		await WindowHelper.WaitFor(condition, timeoutMS: (int)CompletionTimeout.TotalMilliseconds);
	}

	private static async Task WaitForFrames(int count)
	{
		for (var i = 0; i < count; i++)
		{
			await WindowHelper.WaitForIdle();
			await Task.Delay(16);
		}
	}
}
