using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.FramePages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_AppBarButton
	{
		[TestMethod]
		public async Task Check_DataContext_Propagation()
		{
			using (StyleHelper.UseNativeFrameNavigation())
			{
				var frame = new Frame();
				WindowHelper.WindowContent = frame;
				await WindowHelper.WaitForIdle();
				frame.Navigate(typeof(Page_With_AppBarButton_Visibility_Bound));
				await WindowHelper.WaitForIdle();
				var page = frame.Content as Page_With_AppBarButton_Visibility_Bound;
				Assert.IsNotNull(page);
				page.DataContext = new MyContext();
				await WindowHelper.WaitForIdle();
				var tb = page.innerTextBlock;
				Assert.IsNotNull(tb);
				Assert.AreEqual("Archaeopteryx", tb.Text);
				Assert.IsTrue(tb.ActualWidth > 0);
				Assert.IsTrue(tb.ActualHeight > 0);
			}
		}

		private class MyContext
		{
			public Visibility ButtonVisibility => Visibility.Visible;
			public string ButtonText => "Archaeopteryx";
		}
	}
}
