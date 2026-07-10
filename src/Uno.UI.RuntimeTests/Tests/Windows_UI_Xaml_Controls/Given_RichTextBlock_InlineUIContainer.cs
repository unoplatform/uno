using System.Threading.Tasks;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Windows.Foundation;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RichTextBlock_InlineUIContainer
	{
		private static Border CreateChild(double width, double height) => new()
		{
			Width = width,
			Height = height,
			Background = new SolidColorBrush(Colors.Red),
		};

		private static RichTextBlock CreateSUT(UIElement child, string before = "AB", string after = "CD")
		{
			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = before });
			paragraph.Inlines.Add(new InlineUIContainer { Child = child });
			paragraph.Inlines.Add(new Run { Text = after });

			var SUT = new RichTextBlock { FontSize = 20 };
			SUT.Blocks.Add(paragraph);
			return SUT;
		}

		[TestMethod]
		public async Task When_Child_Is_Measured_And_Arranged()
		{
			var child = CreateChild(40, 20);
			var SUT = CreateSUT(child);

			await UITestHelper.Load(SUT);

			Assert.AreEqual(40, child.ActualWidth);
			Assert.AreEqual(20, child.ActualHeight);

			// The child sits after the leading run, so it cannot be at the very left of the control.
			var offset = child.TransformToVisual(SUT).TransformPoint(new Point(0, 0));
			Assert.IsTrue(offset.X > 0, $"Expected the child to be positioned after the leading run, but X was {offset.X}.");
		}

		[TestMethod]
		public async Task When_Container_Reserves_Space_In_Line()
		{
			var withContainer = CreateSUT(CreateChild(40, 20));

			var paragraph = new Paragraph();
			paragraph.Inlines.Add(new Run { Text = "AB" });
			paragraph.Inlines.Add(new Run { Text = "CD" });
			var withoutContainer = new RichTextBlock { FontSize = 20 };
			withoutContainer.Blocks.Add(paragraph);

			var panel = new StackPanel();
			panel.Children.Add(withContainer);
			panel.Children.Add(withoutContainer);
			await UITestHelper.Load(panel);

			var extraWidth = withContainer.DesiredSize.Width - withoutContainer.DesiredSize.Width;
			Assert.AreEqual(40, extraWidth, 1, $"The container should widen the line by its child's width, but it grew by {extraWidth}.");
		}

		[TestMethod]
		public async Task When_Tall_Child_Raises_Line_Height()
		{
			var shortChild = CreateSUT(CreateChild(40, 10));
			var tallChild = CreateSUT(CreateChild(40, 80));

			var panel = new StackPanel();
			panel.Children.Add(shortChild);
			panel.Children.Add(tallChild);
			await UITestHelper.Load(panel);

			// The 80px child cannot fit in a 20pt text line, so it must stretch the line it sits on.
			Assert.IsTrue(
				tallChild.DesiredSize.Height >= 80,
				$"Expected the tall child to stretch the line to at least 80px, but the control was {tallChild.DesiredSize.Height}px.");
			Assert.IsTrue(
				tallChild.DesiredSize.Height > shortChild.DesiredSize.Height,
				$"Expected the tall child ({tallChild.DesiredSize.Height}px) to produce a taller control than the short one ({shortChild.DesiredSize.Height}px).");
		}

		[TestMethod]
		public async Task When_Child_Is_Reparented_To_The_Control()
		{
			var child = CreateChild(40, 20);
			var SUT = CreateSUT(child);

			await UITestHelper.Load(SUT);

			// The child is hosted by the RichTextBlock itself, which is what renders and hit-tests it.
			Assert.AreEqual(SUT, VisualTreeHelper.GetParent(child));
		}

		[TestMethod]
		public async Task When_Container_Wraps_To_Next_Line()
		{
			// A child too wide to share the line with the leading run must move to the next line whole.
			var child = CreateChild(90, 20);
			var SUT = CreateSUT(child, before: "AAAA", after: "");
			SUT.TextWrapping = TextWrapping.Wrap;
			SUT.Width = 100;

			await UITestHelper.Load(SUT);

			var offset = child.TransformToVisual(SUT).TransformPoint(new Point(0, 0));
			Assert.AreEqual(0, offset.X, 1, $"Expected the wrapped child to start the line, but X was {offset.X}.");
			Assert.IsTrue(offset.Y > 0, $"Expected the child to wrap onto a second line, but Y was {offset.Y}.");
		}
	}
}
