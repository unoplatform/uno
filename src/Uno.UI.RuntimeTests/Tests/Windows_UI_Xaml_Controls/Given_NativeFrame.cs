#if __ANDROID__ || __IOS__

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
using SamplesApp.UITests.TestFramework;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Markup;
using Uno.UI.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_NativeFrame
	{
#if __ANDROID__
		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
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
		[RequiresFullWindow]
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
#endif
#if __IOS__
		[TestMethod]
		[RunsOnUIThread]
		[RequiresFullWindow]
		public async Task When_Altering_BackStack()
		{
			var style = (Style)XamlReader.Load(@"
				<Style xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
						xmlns:uc=""using:Uno.UI.Controls""
						xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
						TargetType=""Frame"">
				<Setter Property=""Template"">
					<Setter.Value>
						<ControlTemplate TargetType=""Frame"">
							<uc:NativeFramePresenter x:Name=""NativePresenter"" Background=""{TemplateBinding Background}"" />
						</ControlTemplate>
					</Setter.Value>
				</Setter>
			</Style>");

			var SUT = new Frame()
			{
				Style = style
			};

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();

			var presenter = SUT.FindName("NativePresenter") as NativeFramePresenter;

			SUT.Navigate(typeof(MyPage));

			await TestServices.WindowHelper.WaitForIdle();

			//Simulate a deep-link navigation. Nav directly to MyThirdPage and add the logical previous pages into the BackStack manually
			SUT.Navigate(typeof(MyThirdPage));
			SUT.BackStack.Clear();
			SUT.BackStack.Add(new PageStackEntry(typeof(MyFirstPage), null, null));
			SUT.BackStack.Add(new PageStackEntry(typeof(MySecondPage), null, null));

			await TestServices.WindowHelper.WaitForIdle();

			await Task.Delay(1000);

			Assert.AreEqual(SUT.CurrentEntry.Instance, presenter.NavigationController.TopViewController.View as Page);
			Assert.AreEqual(typeof(MyThirdPage), SUT.CurrentSourcePageType);
			AssertBackStackMatchesNavigationControllerStack();

			SUT.GoBack();
			await TestServices.WindowHelper.WaitForIdle();

			await Task.Delay(1000);

			Assert.AreEqual(SUT.CurrentEntry.Instance, presenter.NavigationController.TopViewController.View as Page);
			Assert.AreEqual(typeof(MySecondPage), SUT.CurrentSourcePageType);
			AssertBackStackMatchesNavigationControllerStack();


			SUT.GoBack();
			await TestServices.WindowHelper.WaitForIdle();

			await Task.Delay(1000);

			Assert.AreEqual(SUT.CurrentEntry.Instance, presenter.NavigationController.TopViewController.View as Page);
			Assert.AreEqual(typeof(MyFirstPage), SUT.CurrentSourcePageType);
			AssertBackStackMatchesNavigationControllerStack();

			void AssertBackStackMatchesNavigationControllerStack()
			{
				CollectionAssert.AreEqual(
					SUT.BackStack.Select(x => x.Instance).ToArray(),
					presenter.NavigationController.ViewControllers.SkipLast(1).Select(x => x.View).ToArray());
			}
		}
#endif
	}
	partial class MyPage : Page
	{
	}
	partial class MyFirstPage : Page
	{
	}
	partial class MySecondPage : Page
	{
	}
	partial class MyThirdPage : Page
	{
	}
}
#endif
