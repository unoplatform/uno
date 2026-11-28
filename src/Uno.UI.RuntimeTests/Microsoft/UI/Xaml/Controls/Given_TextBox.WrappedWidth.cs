using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

#if WINAPPSDK
using Uno.UI.Extensions;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_TextBox
	{
		// A line whose spaces don't fit in the box: they don't wrap, so they hang past its right edge.
		private static readonly string HangingSpacesText = "aaaa" + new string(' ', 80) + "bbbb";
		private const int HangingSpacesEnd = 84;

		[TestMethod]
		[DataRow(TextAlignment.Left, FlowDirection.LeftToRight)]
		[DataRow(TextAlignment.Center, FlowDirection.LeftToRight)]
		[DataRow(TextAlignment.Right, FlowDirection.LeftToRight)]
		[DataRow(TextAlignment.Left, FlowDirection.RightToLeft)]
		public async Task When_Wrapped_Line_Ends_In_Spaces_Then_Not_Scrollable_Horizontally(TextAlignment alignment, FlowDirection flowDirection)
		{
			var SUT = new TextBox
			{
				Width = 200,
				TextWrapping = TextWrapping.Wrap,
				AcceptsReturn = true,
				TextAlignment = alignment,
				FlowDirection = flowDirection,
				Text = HangingSpacesText,
			};

			await UITestHelper.Load(SUT);
			var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
			Assert.IsNotNull(scrollViewer);

			AssertNotScrollableHorizontally(scrollViewer);

			// A caret among the hanging spaces doesn't scroll the text either.
			SUT.Focus(FocusState.Programmatic);
			SUT.Select(HangingSpacesEnd - 20, 0);
			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, scrollViewer.HorizontalOffset, "A caret among a line's hanging spaces should not scroll the text.");
			AssertNotScrollableHorizontally(scrollViewer);
		}

		[TestMethod]
		public async Task When_NoWrap_Line_Ends_In_Spaces_Then_Scrollable_Horizontally()
		{
			var SUT = new TextBox
			{
				Width = 200,
				TextWrapping = TextWrapping.NoWrap,
				Text = HangingSpacesText,
			};

			await UITestHelper.Load(SUT);
			var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
			Assert.IsNotNull(scrollViewer);

			Assert.IsTrue(scrollViewer.ScrollableWidth > 0, $"An unwrapped line wider than the box should scroll (ScrollableWidth={scrollViewer.ScrollableWidth}).");
		}

		[TestMethod]
		public async Task When_Wrapped_Auto_Width_In_Left_Aligned_StackPanel()
		{
			var SUT = new TextBox
			{
				TextWrapping = TextWrapping.Wrap,
				AcceptsReturn = true,
				Text = HangingSpacesText,
			};
			var host = new Border
			{
				Width = 200,
				Child = new StackPanel
				{
					HorizontalAlignment = HorizontalAlignment.Left,
					Children = { SUT },
				},
			};

			await UITestHelper.Load(host);
			var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
			Assert.IsNotNull(scrollViewer);

			Assert.AreEqual(200, SUT.ActualWidth, 0.5, "A wrapped box whose text is wider than its slot should take the slot's width.");
			AssertNotScrollableHorizontally(scrollViewer);
		}

		[TestMethod]
		public async Task When_Wrapped_Auto_Width_In_Auto_Grid_Column()
		{
			var SUT = new TextBox
			{
				TextWrapping = TextWrapping.Wrap,
				AcceptsReturn = true,
				Text = "hello world   ",
			};
			var reference = new TextBox
			{
				TextWrapping = TextWrapping.Wrap,
				AcceptsReturn = true,
				Text = "hello world",
			};
			var host = new StackPanel
			{
				HorizontalAlignment = HorizontalAlignment.Left,
				Children =
				{
					new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto } }, Children = { SUT } },
					new Grid { ColumnDefinitions = { new ColumnDefinition { Width = GridLength.Auto } }, Children = { reference } },
				},
			};

			await UITestHelper.Load(host);
			var scrollViewer = SUT.FindFirstChild<ScrollViewer>(viewer => viewer.Name == "ContentElement");
			Assert.IsNotNull(scrollViewer);

			// With no width to wrap at, the box takes its text's width, trailing spaces included.
			Assert.IsTrue(SUT.ActualWidth > reference.ActualWidth, $"The trailing spaces should widen an auto-width wrapped box ({SUT.ActualWidth} vs {reference.ActualWidth} without them).");
			AssertNotScrollableHorizontally(scrollViewer);
		}

		private static void AssertNotScrollableHorizontally(ScrollViewer scrollViewer)
		{
			Assert.AreEqual(0, scrollViewer.ScrollableWidth, $"A wrapped box should not scroll horizontally (ExtentWidth={scrollViewer.ExtentWidth}, ViewportWidth={scrollViewer.ViewportWidth}).");
			Assert.IsTrue(scrollViewer.ExtentWidth <= scrollViewer.ViewportWidth, $"ExtentWidth={scrollViewer.ExtentWidth} should not exceed ViewportWidth={scrollViewer.ViewportWidth}.");
		}
	}
}
