using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using FluentAssertions;
using FluentAssertions.Execution;
using static Private.Infrastructure.TestServices;
using Uno.UI.Extensions;
using Windows.UI.Xaml.Controls.Primitives;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Extensions;
using Windows.ApplicationModel.Background;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_Page
	{
		private const string Red = "#FFFF0000";
		private const string Blue = "#FF0000FF";
		private const string Green = "#FF008000";

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Background_Static()
		{
			var SUT = new Page_Automated();
	
			var content = await RawBitmap.TakeScreenshot(SUT);
			var emptyRect = SUT.GetRelativeCoords(SUT._empty);
			var transparentRect = SUT.GetRelativeCoords(SUT._transparent);
			var solidRect = SUT.GetRelativeCoords(SUT._colored);
			var offset = 30; 
			ImageAssert.HasColorAt(content, (emptyRect.X + offset), (emptyRect.Y + offset), Red);
			ImageAssert.HasColorAt(content, (transparentRect.X + offset), (transparentRect.Y + offset), Red);
			ImageAssert.HasColorAt(content, (solidRect.X + offset), (solidRect.Y + offset), Blue);
		}

		[TestMethod]
		[RunsOnUIThread]
		public async Task When_Background_Updated()
		{
			var SUT = new Page_UpdateBackground();
			WindowHelper.WindowContent = SUT;
			var before = await RawBitmap.TakeScreenshot(SUT);

			var rect = SUT.GetRelativeCoords(SUT.TargetPage);
			
			ImageAssert.HasColorAt(before, rect.CenterX, rect.CenterY, Blue);

			SUT.AdvanceTestButton_Click();
			await WindowHelper.WaitForIdle();

			var after = await RawBitmap.TakeScreenshot(SUT);
			ImageAssert.HasColorAt(after, rect.CenterX, rect.CenterY, Green);
		}
	}
}
