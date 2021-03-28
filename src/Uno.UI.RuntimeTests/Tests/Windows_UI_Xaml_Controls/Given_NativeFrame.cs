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
	}

	internal static class FrameExtensions
	{
		public static int GetAllMyPages(this Frame sut) => sut.EnumerateAllChildren(v => v is MyPage).Count();

		/// Actively waiting for pages to be stacked is
		/// required as NativeFramePresenter.UpdateStack awaits
		/// for animations to finish, and there's no way to determine
		/// from the Frame PoV that the animation is finished.
		public static async Task WaitForPages(this Frame frame, int count)
		{
			var sw = Stopwatch.StartNew();

			while (sw.Elapsed < TimeSpan.FromSeconds(5))
			{
				await TestServices.WindowHelper.WaitForIdle();

				if (frame.GetAllMyPages() == count)
				{
					break;
				}
			}

			Assert.AreEqual(count, frame.GetAllMyPages());
		}
	}

	partial class MyPage : Page
	{
	}
}
#endif
