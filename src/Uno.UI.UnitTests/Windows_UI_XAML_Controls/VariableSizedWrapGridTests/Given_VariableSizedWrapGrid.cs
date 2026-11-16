using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;

namespace Uno.UI.Tests.VariableSizedWrapGridTests
{
	[UITestClass]
	public class Given_VariableSizedWrapGrid
	{
		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26425")]
		public void When_Items_Have_Zero_Size_Then_Layout_Does_Not_Throw()
		{
			var panel = new VariableSizedWrapGrid { Width = 300, Height = 300, Orientation = Orientation.Horizontal };
			for (var i = 0; i < 4; i++)
			{
				var border = new Border();
				if (i == 1)
				{
					VariableSizedWrapGrid.SetRowSpan(border, 2);
				}
				panel.Children.Add(border);
			}

			panel.Measure(new Size(300, 300));
			panel.Arrange(new Rect(0, 0, 300, 300));

			foreach (var child in panel.Children)
			{
				Assert.AreEqual(new Size(0, 0), child.RenderSize);
			}
		}

		[TestMethod]
		[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26425")]
		public void When_Items_Have_Zero_Size_And_Spans_Then_Layout_Does_Not_Throw()
		{
			var panel = new VariableSizedWrapGrid { Width = 300, Height = 300, MaximumRowsOrColumns = -1 };
			for (var i = 0; i < 4; i++)
			{
				var border = new Border();
				VariableSizedWrapGrid.SetRowSpan(border, 2);
				VariableSizedWrapGrid.SetColumnSpan(border, 2);
				panel.Children.Add(border);
			}

			panel.Measure(new Size(300, 300));
			panel.Arrange(new Rect(0, 0, 300, 300));

			foreach (var child in panel.Children)
			{
				Assert.AreEqual(new Size(0, 0), child.RenderSize);
			}
		}
	}
}
