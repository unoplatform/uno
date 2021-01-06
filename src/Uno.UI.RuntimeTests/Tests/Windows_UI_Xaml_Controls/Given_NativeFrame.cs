
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
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.FramePages;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_NativeFrame
	{
#if __ANDROID__
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_NavigateForward()
		{
			var style = Windows.UI.Xaml.Application.Current.Resources["NativeDefaultFrame"] as Style;
			Assert.IsNotNull(style);

			var SUT = new Frame() {
				Style = style
			};

			TestServices.WindowHelper.WindowContent = SUT;

			int GetAllMyPages()
				=> SUT.EnumerateAllChildren(v => v is MyPage).Count();

			/// Actively waiting for pages to be stacked is
			/// required as NativeFramePresenter.UpdateStack awaits
			/// for animations to finish, and there's no way to determine
			/// from the Frame PoV that the animation is finished.
			async Task WaitForPages(int count)
			{
				var sw = Stopwatch.StartNew();

				while(sw.Elapsed < TimeSpan.FromSeconds(5))
				{
					await TestServices.WindowHelper.WaitForIdle();

					if (GetAllMyPages() == count)
					{
						break;
					}
				}

				Assert.AreEqual(count, GetAllMyPages());
			}

			await WaitForPages(0);

			SUT.Navigate(typeof(MyPage));

			await WaitForPages(1);

			SUT.Navigate(typeof(MyPage));

			await WaitForPages(2);

			SUT.GoBack();

			await WaitForPages(1);
		}
#endif
		[TestMethod]
		[RunsOnUIThread]
		public async Task When_ContentIsNullAndNavigate()
		{
#if !NETFX_CORE
			var style = Windows.UI.Xaml.Application.Current.Resources["NativeDefaultFrame"] as Style;
			Assert.IsNotNull(style);

			var SUT = new Frame()
			{
				Style = style
			};
#else
			var SUT = new Frame();
#endif

			TestServices.WindowHelper.WindowContent = SUT;

			SUT.Navigate(typeof(FirstPage));

			var firstPage = SUT.Content as FirstPage;

			Assert.IsNotNull(firstPage.FirstTextBlock);
			Assert.AreEqual("Page The First", firstPage.FirstTextBlock.Text);

			SUT.Content = null;
			SUT.BackStack?.Clear();


			SUT.Navigate(typeof(SecondPage));
			var secondPage = SUT.Content as SecondPage;

			Assert.IsNotNull(SUT.BackStack.FirstOrDefault());
			Assert.AreEqual(typeof(FirstPage), SUT.BackStack.FirstOrDefault().SourcePageType);

			Assert.IsNotNull(secondPage.SecondTextBlock);
			Assert.AreEqual("Page The Second", secondPage.SecondTextBlock.Text);
		}
	}

	partial class MyPage : Page
	{
	}
}
