using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ScrollViewer
	{
#if __SKIA__ || __WASM__
		[TestMethod]
		public async Task When_CreateVerticalScroller_Then_DoNotLoadAllTemplate()
		{
			var sut = new ScrollViewer
			{
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				VerticalScrollMode = ScrollMode.Enabled,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
				HorizontalScrollMode = ScrollMode.Disabled,
				Height = 100,
				Width = 100,
				Content = new Border {Height = 200, Width = 50}
			};
			WindowHelper.WindowContent = sut;

			await WindowHelper.WaitForIdle();

			var buttons = sut
				.EnumerateAllChildren(maxDepth: 256)
				.OfType<RepeatButton>()
				.Count();

			Assert.IsTrue(buttons > 0); // We make sure that we really loaded the right template
			Assert.IsTrue(buttons <= 4);
		}
#endif
	}
}
