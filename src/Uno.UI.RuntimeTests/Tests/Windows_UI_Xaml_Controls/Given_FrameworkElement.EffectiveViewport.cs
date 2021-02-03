using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using FluentAssertions;
using FluentAssertions.Execution;
using Private.Infrastructure;

namespace Uno.UI.Tests.Windows_UI_Xaml.FrameworkElementTests
{
	partial class Given_FrameworkElement
	{
		[TestMethod]
		public async Task EffectiveViewport_When_BottomRightAligned()
		{
			var sut = new Border {Width = 42, Height = 42, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom};
			var parent = new ScrollViewer {Width = 142, Height = 142, Content = new Grid {Children = {sut}}};

			var result = Rect.Empty;
			sut.EffectiveViewportChanged += (snd, e) => result = e.EffectiveViewport;

			TestServices.WindowHelper.WindowContent = parent;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.AreEqual(new Rect(-100, -100, 142, 142), result);
		}
	}
}
