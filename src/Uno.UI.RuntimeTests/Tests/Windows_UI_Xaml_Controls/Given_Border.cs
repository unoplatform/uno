using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Border
	{
		[TestMethod]
		public async Task Check_DataContext_Propagation()
		{
			var tb = new TextBlock();
			tb.SetBinding(TextBlock.TextProperty, new Binding { Path = new PropertyPath("TestText") });
			var SUT = new Border
			{
				Child = tb
			};

			var root = new Grid
			{
				DataContext = new MyContext()
			};

			root.Children.Add(SUT);

			WindowHelper.WindowContent = root;

			await WindowHelper.WaitFor(() => tb.Text == "Vampire squid");
		}

		private class MyContext
		{
			public string TestText => "Vampire squid";
		}

		[TestMethod]
		public async Task When_Border_Centered_With_Margin_Inside_Tall_Rectangle()
		{
			const int ContentHeight = 300;
			const int ContentMargin = 10;
			var content = new Border
			{
				Width = 300,
				Height = ContentHeight,
				Margin = new Thickness(ContentMargin),
				Background = new SolidColorBrush(Colors.DeepPink)
			};
			var SUT = new Border
			{
				Background = new SolidColorBrush(Colors.Pink),
				Width = 50,
				Height = double.NaN,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Child = content
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(content);

			const double ScrollViewerHeight = ContentHeight + 2 * ContentMargin;
			await WindowHelper.WaitForEqual(ScrollViewerHeight, () => SUT.ActualHeight);
		}

		[TestMethod]
		public async Task When_Border_Centered_With_Margin_Inside_Wide_Rectangle()
		{
			const int ContentWidth = 300;
			const int ContentMargin = 10;
			var content = new Border
			{
				Height = 300,
				Width = ContentWidth,
				Margin = new Thickness(ContentMargin),
				Background = new SolidColorBrush(Colors.DeepPink)
			};
			var SUT = new Border
			{
				Background = new SolidColorBrush(Colors.Pink),
				Height = 50,
				Width = double.NaN,
				HorizontalAlignment = HorizontalAlignment.Center,
				VerticalAlignment = VerticalAlignment.Center,
				Child = content
			};

			WindowHelper.WindowContent = SUT;

			await WindowHelper.WaitForLoaded(content);

			const double ScrollViewerWidth = ContentWidth + 2 * ContentMargin;
			await WindowHelper.WaitForEqual(ScrollViewerWidth, () => SUT.ActualWidth);
		}
	}
}
