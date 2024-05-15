using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using SamplesApp.UITests.TestFramework;
using Uno.UITest.Helpers;
using Uno.UITest.Helpers.Queries;

namespace SamplesApp.UITests.Windows_UI_Xaml_Controls.CalendarViewTests
{
	[TestFixture]
	public partial class CalendarViewTests : SampleControlUITestBase
	{
		[Test]
		[AutoRetry]
		[ActivePlatforms(Platform.iOS, Platform.Browser)]
		public void When_Theme_Changed_No_Crash()
		{
			Run("UITests.Windows_UI_Xaml_Controls.CalendarView.CalendarView_Theming");

			var isLight = IsLight();

			// Do couple of toggles
			for (int i = 0; i < 4; i++)
			{
				_app.FastTap("DarkLightModeToggle");
				var newIsLight = IsLight();
				Assert.AreNotEqual(isLight, newIsLight);
				isLight = !isLight;
			}

			bool IsLight()
			{
				var sourceProperty = _app.Marked("UnoLogoImage").GetDependencyPropertyValue<string>("Source");
				if (sourceProperty.Contains("UnoGalleryLogo_Light"))
				{
					return true;
				}
				else if (sourceProperty.Contains("UnoGalleryLogo_Dark"))
				{
					return false;
				}

				throw new InvalidOperationException("Expected Source property to contain either UnoGalleryLogo_Light or UnoGalleryLogo_Dark. Found " + sourceProperty);
			}
		}
	}
}
