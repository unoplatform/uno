using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.Extensions;

#if WINDOWS_UWP
using Windows.UI.Xaml.Controls.Primitives;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.Repeater
{
	[TestClass]
	public class Given_ItemsRepeater
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_NoScrollViewer_Then_ShowMoreThanFirstItem()
		{
			var sut = new ItemsRepeater
			{
				ItemsSource = new[] {"Item_1", "Item_2"}
			};
			var popup = new Popup
			{
				Child = new Grid
				{
					Width = 100,
					Height = 200,
					Children = {sut}
				}
			};

			TestServices.WindowHelper.WindowContent = popup;
			await TestServices.WindowHelper.WaitForIdle();

			popup.IsOpen = true;

			await TestServices.WindowHelper.WaitForIdle();
			sut.UpdateLayout();

			var second = sut
				.GetAllChildren()
				.OfType<TextBlock>()
				.FirstOrDefault(t => t.Text == "Item_2");

			popup.IsOpen = false;
			TestServices.WindowHelper.WindowContent = null;

			Assert.IsNotNull(second);
		}
	}
}
