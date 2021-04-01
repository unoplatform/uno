#if __ANDROID__
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml;
using System.Threading.Tasks;
using System.Diagnostics;
using Uno.UI.RuntimeTests.Extensions;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_NativeFrame
	{
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_NavigateForward()
		{
			var style = Windows.UI.Xaml.Application.Current.Resources["NativeDefaultFrame"] as Style;
			Assert.IsNotNull(style);

			var SUT = new Frame()
			{
				Style = style
			};

			TestServices.WindowHelper.WindowContent = SUT;

			await SUT.WaitForPages(0);

			SUT.Navigate(typeof(MyPage));

			await SUT.WaitForPages(1);

			SUT.Navigate(typeof(MyPage));

			await SUT.WaitForPages(2);

			SUT.GoBack();

			await SUT.WaitForPages(1);
		}


		[TestMethod]
		[RunsOnUIThread]
		public async Task When_NavigateBackSkipingPages()
		{
			var style = Windows.UI.Xaml.Application.Current.Resources["NativeDefaultFrame"] as Style;
			Assert.IsNotNull(style);

			var SUT = new Frame()
			{
				Style = style
			};

			TestServices.WindowHelper.WindowContent = SUT;

			await SUT.WaitForPages(0);

			SUT.Navigate(typeof(MyPage));

			await SUT.WaitForPages(1);

			SUT.Navigate(typeof(MyPage));

			await SUT.WaitForPages(2);

			SUT.Navigate(typeof(MyPage));

			await SUT.WaitForPages(3);

			//Remove the previous page to jump from page 3 to page 1
			SUT.BackStack.Remove(SUT.BackStack.LastOrDefault());

			SUT.GoBack();

			await SUT.WaitForPages(1);
		}
	}
	partial class MyPage : Page
	{
	}
}
#endif
