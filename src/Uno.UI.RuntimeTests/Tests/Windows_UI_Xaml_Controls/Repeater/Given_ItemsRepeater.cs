using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using FluentAssertions;
using Uno.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater
{
	[TestClass]
	public class Given_ItemsRepeater
	{
		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_NoScrollViewer_Then_ShowMoreThanFirstItem()
		{
			var sut = new ItemsRepeater
			{
				ItemsSource = new[] { "Item_1", "Item_2" }
			};
			var popup = new Popup
			{
				Child = new Grid
				{
					Width = 100,
					Height = 200,
					Children = { sut }
				}
			};

			TestServices.WindowHelper.WindowContent = popup;
			await TestServices.WindowHelper.WaitForIdle();

			popup.IsOpen = true;

			await TestServices.WindowHelper.WaitForIdle();
			sut.UpdateLayout();

			try
			{
				await RetryAssert(() =>
				{
					var second = sut
						.GetAllChildren()
						.OfType<TextBlock>()
						.FirstOrDefault(t => t.Text == "Item_2");

					Assert.IsNotNull(second);
				});
			}
			finally
			{
				popup.IsOpen = false;
				TestServices.WindowHelper.WindowContent = null;
			}
		}

#if HAS_UNO
		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_NestedInSVAndOutOfViewportOnInitialLoad_Then_MaterializedEvenWhenScrollingOnMinorAxis()
		{
			var sut = default(ItemsRepeater);
			var sv = new ScrollViewer
			{
				Content = new StackPanel
				{
					Children = {
						new Border { Background = new SolidColorBrush(Colors.DeepPink), Height = 8192, Width = 150 },
						(sut = new ItemsRepeater
						{
							ItemsSource = Enumerable.Range(0, 10).Select(i => $"Item #{i}"),
							Layout = new StackLayout { Orientation = Orientation.Horizontal },
							ItemTemplate = new DataTemplate(() => new Border
							{
								Width = 100,
								Height = 100,
								Background = new SolidColorBrush(Colors.DeepSkyBlue),
								Margin = new Thickness(10),
								Child = new TextBlock().Apply(tb => tb.SetBinding(TextBlock.TextProperty, new Binding()))
							})
						})
					}
				}
			};

			TestServices.WindowHelper.WindowContent = sv;
			await TestServices.WindowHelper.WaitForIdle();

#if !__IOS__
			sut.Children.Count.Should().BeLessOrEqualTo(1);
#endif

			sv.ChangeView(null, sv.ExtentHeight, null, disableAnimation: true);

			await TestServices.WindowHelper.WaitForIdle();

			sut.Children.Count.Should().BeGreaterThan(1);
		}
#endif

		private async Task RetryAssert(Action assertion)
		{
			var attempt = 0;
			while (true)
			{
				try
				{
					assertion();

					break;
				}
				catch (Exception)
				{
					if (attempt++ >= 30)
					{
						throw;
					}

					await Task.Delay(10);
				}
			}
		}
	}
}
