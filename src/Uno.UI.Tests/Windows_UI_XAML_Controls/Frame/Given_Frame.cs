using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.Tests.FrameTests
{
	[TestClass]
	public class Given_Frame
	{
		[TestInitialize]
		public void Init()
		{
			UnitTestsApp.App.EnsureApplication();
		}

		[TestMethod]
		public void When_RemovedPage()
		{
			var SUT = new Frame()
			{
			};

			SUT.Navigate(typeof(MyPage));

			var myPage1 = SUT.Content as MyPage;
			Assert.IsNotNull(myPage1);
			Assert.AreEqual(SUT, myPage1.Frame);

			SUT.Navigate(typeof(MyPage));

			var myPage2 = SUT.Content as MyPage;
			Assert.IsNotNull(myPage2);
			Assert.AreEqual(SUT, myPage2.Frame);

			SUT.GoBack();

			Assert.AreEqual(myPage1, SUT.Content);
			Assert.IsNotNull(myPage2.Frame);

			SUT.Navigate(typeof(MyPage));

			var myPage3 = SUT.Content as MyPage;

			Assert.AreEqual(myPage3, SUT.Content);
			Assert.IsNull(myPage2.Frame);
		}

		internal static NavigateOrderTracker _navigateOrderTracker = null;

		[TestMethod]
		public void When_NavigatingBetweenPages()
		{
			//must use static field, as page is created internally by the frame
			_navigateOrderTracker = null;

			var SUT = new Frame()
			{
			};

			SUT.Navigated += (s, e) =>
			{
				_navigateOrderTracker?.OnFrameNavigated();
			};

			SUT.Navigate(typeof(NavigationTrackingPage));

			_navigateOrderTracker = new NavigateOrderTracker();

			SUT.Navigate(typeof(NavigationTrackingPage));

			//now all should be set
			Assert.IsTrue(_navigateOrderTracker.NavigatedFrom);
			Assert.IsTrue(_navigateOrderTracker.NavigatedTo);
			Assert.IsTrue(_navigateOrderTracker.FrameNavigated);
		}
	}

	class MyPage : Page
	{
	}

	class NavigateOrderTracker
	{
		public bool NavigatedFrom { get; set; }

		public bool NavigatedTo { get; set; }

		public bool FrameNavigated { get; set; }

		public void OnPageNavigatedFrom()
		{
			//after frame navigated
			Assert.IsTrue(FrameNavigated);
			Assert.IsFalse(NavigatedFrom);
			Assert.IsFalse(NavigatedTo);

			NavigatedFrom = true;
		}

		public void OnFrameNavigated()
		{
			//first event
			Assert.IsFalse(FrameNavigated);
			Assert.IsFalse(NavigatedFrom);
			Assert.IsFalse(NavigatedTo);

			FrameNavigated = true;
		}

		public void OnPageNavigatedTo()
		{
			//last event
			Assert.IsTrue(FrameNavigated);
			Assert.IsTrue(NavigatedFrom);
			Assert.IsFalse(NavigatedTo);

			NavigatedTo = true;
		}
	}

	class NavigationTrackingPage : Page
	{
		protected internal override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			Given_Frame._navigateOrderTracker?.OnPageNavigatedTo();
		}

		protected internal override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			Given_Frame._navigateOrderTracker?.OnPageNavigatedFrom();
		}
	}
}
