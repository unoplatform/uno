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
		public void When_Navigating_Cancels()
		{
			// Arrange
			var SUT = new Frame()
			{
			};

			SUT.Navigate(typeof(DisallowNavigatingFromPage));

			// Reset flag
			DisallowNavigatingFromPage.NavigatingFromCalled = false;

			// Set events for next navigation
			SUT.Navigating += (sender, args) => args.Cancel = true;
			SUT.Navigated += (sender, args) => Assert.Fail(); // navigation cannot happen

			// Act
			SUT.Navigate(typeof(MyPage));

			// Assert
			Assert.IsFalse(DisallowNavigatingFromPage.NavigatingFromCalled);
			Assert.IsInstanceOfType(SUT.Content, typeof(DisallowNavigatingFromPage));
		}

		[TestMethod]
		public void When_IsNavigationStackEnabled_False()
		{
			// Arrange
			var SUT = new Frame()
			{
			};

			SUT.IsNavigationStackEnabled = false;
			SUT.Navigate(typeof(MyPage));

			Assert.AreEqual(0, SUT.BackStack.Count);
			Assert.IsFalse(SUT.CanGoBack);
		}

		[TestMethod]
		public void When_IsNavigationStackEnabled_False_After_Start()
		{
			// Arrange
			var SUT = new Frame()
			{
			};

			SUT.Navigate(typeof(FirstPage));

			SUT.Navigate(typeof(SecondPage));

			SUT.Navigate(typeof(ThirdPage));

			Assert.AreEqual(2, SUT.BackStack.Count);

			SUT.GoBack();

			SUT.IsNavigationStackEnabled = false;

			Assert.AreEqual(1, SUT.BackStack.Count);
			Assert.AreEqual(1, SUT.ForwardStack.Count);

			SUT.Navigate(typeof(MyPage));
			SUT.Navigate(typeof(MyPage));

			Assert.IsInstanceOfType(SUT.Content, typeof(MyPage));
			Assert.AreEqual(1, SUT.BackStack.Count);
			Assert.AreEqual(1, SUT.ForwardStack.Count);

			SUT.GoBack();

			Assert.IsInstanceOfType(SUT.Content, typeof(FirstPage));
			Assert.AreEqual(1, SUT.BackStack.Count);
			Assert.AreEqual(1, SUT.ForwardStack.Count);

			SUT.GoForward();

			Assert.IsInstanceOfType(SUT.Content, typeof(ThirdPage));
			Assert.AreEqual(1, SUT.BackStack.Count);
			Assert.AreEqual(1, SUT.ForwardStack.Count);
		}

		[TestMethod]
		public void When_IsNavigationStackEnabled_Can_Enable()
		{
			// Arrange
			var SUT = new Frame()
			{
			};

			SUT.Navigate(typeof(FirstPage));

			SUT.Navigate(typeof(SecondPage));

			SUT.Navigate(typeof(ThirdPage));

			SUT.GoBack();

			SUT.IsNavigationStackEnabled = false;

			SUT.GoBack();

			Assert.IsInstanceOfType(SUT.Content, typeof(FirstPage));
			Assert.AreEqual(1, SUT.BackStack.Count);
			Assert.AreEqual(1, SUT.ForwardStack.Count);

			SUT.IsNavigationStackEnabled = true;
			SUT.GoBack();

			Assert.IsInstanceOfType(SUT.Content, typeof(FirstPage));
			Assert.AreEqual(0, SUT.BackStack.Count);
			Assert.AreEqual(2, SUT.ForwardStack.Count);
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
	}

	class MyPage : Page
	{
	}

	class FirstPage : Page
	{
	}

	class SecondPage : Page
	{
	}

	class ThirdPage : Page
	{
	}

	class DisallowNavigatingFromPage : Page
	{
		public static bool NavigatingFromCalled = false;

		protected internal override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			NavigatingFromCalled = true;
		}
	}
}
