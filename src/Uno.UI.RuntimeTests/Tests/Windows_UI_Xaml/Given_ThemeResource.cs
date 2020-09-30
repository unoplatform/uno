using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml.Controls;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_ThemeResource
	{
		[TestMethod]
#if NETFX_CORE
		[Ignore("Fails on UWP with 'The parameter is incorrect.'")]
#endif
		public async Task When_ThemeResource_Binding_In_Template()
		{
			using (StyleHelper.UseAppLevelResources(new App_Level_Resources()))
			{
				var userControl = new When_ThemeResource_Binding_In_Template_UserControl();

				WindowHelper.WindowContent = userControl;
				await WindowHelper.WaitForLoaded(userControl);

				Assert.AreEqual(Colors.Red, GetInnerBackgroundColor(userControl.Inner_ThemeResource_Control_No_Override));
				Assert.AreEqual(Colors.Blue, GetInnerBackgroundColor(userControl.Inner_ThemeResource_Control_With_Override));

				Color GetInnerBackgroundColor(Inner_ThemeResource_Control control)
				{
					var brush = control.ThemeBoundBorder?.Background as SolidColorBrush;
					Assert.IsNotNull(brush);
					return brush.Color;
				} 
			}
		}
	}
}
