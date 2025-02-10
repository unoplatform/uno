using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.UI.RuntimeTests.Extensions;

#if WINAPPSDK
using Uno.UI.Extensions;
#elif __APPLE_UIKIT__
using UIKit;
#else
using Uno.UI;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_Viewbox
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ContentShouldCenter_WithMinWidth()
		{
			// all colored elements should be concentric
			var root = (Grid)XamlReader.Load(@"
			<Grid xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'
				  Width='340' Height='180' Background='DeepPink'>
				<Viewbox MaxHeight='160' MinWidth='160'>
					<Grid Width='160' Height='200' Background='Aqua'>
						<Rectangle x:Name='PinkRect' Width='20' Fill='Pink' />
					</Grid>
				</Viewbox>

				<Rectangle x:Name='WhiteRect' Width='8' Margin='0,40' Fill='White' />
			</Grid>
			".Replace('\'', '"'));

			TestServices.WindowHelper.WindowContent = root;
			await TestServices.WindowHelper.WaitForIdle();

			var pinkRect = root.FindFirstChild<Rectangle>(x => x.Name == "PinkRect");
			var whiteRect = root.FindFirstChild<Rectangle>(x => x.Name == "WhiteRect");

			var pinkCoords = pinkRect.GetOnScreenBounds();
			var whiteCoords = whiteRect.GetOnScreenBounds();
			var intersection = RectHelper.Intersect(pinkCoords, whiteCoords);

			Assert.AreEqual(whiteCoords, intersection, "WhiteRect should be contained within PinkRect");
		}
	}
}
