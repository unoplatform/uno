using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Markup;
using Uno.UI.RuntimeTests.Extensions;

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

			var pinkRect = root.FindViewsByName("PinkRect").FirstOrDefault();
			var whiteRect = root.FindViewsByName("WhiteRect").FirstOrDefault();

			var pinkCoords = pinkRect.GetOnScreenBounds();
			var whiteCoords = whiteRect.GetOnScreenBounds();
			var intersection = pinkCoords.IntersectWith(whiteCoords);

			Assert.AreEqual(whiteCoords, intersection, "WhiteRect should be contained within PinkRect");
		}
	}
}
