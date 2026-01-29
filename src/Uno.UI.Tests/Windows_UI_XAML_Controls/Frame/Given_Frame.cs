using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

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

			Assert.IsEmpty(SUT.BackStack);
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
			Assert.IsEmpty(SUT.BackStack);
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

		[TestMethod]
		public void When_Tracking_SourcePageType()
		{
			var SUT = new Frame();

			void OnPageNavigatedFrom(object sender, EventArgs args)
			{
				// Should match the other page type
				Assert.AreNotEqual(sender.GetType(), SUT.SourcePageType);
				Assert.AreNotEqual(sender.GetType(), SUT.CurrentSourcePageType);
			}

			void OnPageNavigatingFrom(object sender, EventArgs args)
			{
				// Should match sender
				Assert.AreEqual(sender.GetType(), SUT.SourcePageType);
				Assert.AreEqual(sender.GetType(), SUT.CurrentSourcePageType);
			}

			void OnPageNavigatedTo(object sender, EventArgs args)
			{
				// Should match sender
				Assert.AreEqual(sender.GetType(), SUT.SourcePageType);
				Assert.AreEqual(sender.GetType(), SUT.CurrentSourcePageType);
			}

			try
			{
				SourceTypePage.PageNavigatedFrom += OnPageNavigatedFrom;
				SourceTypePage.PageNavigatingFrom += OnPageNavigatingFrom;
				SourceTypePage.PageNavigatedTo += OnPageNavigatedTo;

				Type navigatingSourcePageType = null;
				Type navigatingCurrentSourcePageType = null;

				SUT.Navigating += (s, e) =>
				{
					Assert.AreEqual(navigatingSourcePageType, SUT.SourcePageType);
					Assert.AreEqual(navigatingCurrentSourcePageType, SUT.CurrentSourcePageType);
				};

				Type navigatedSourcePageType = null;
				Type navigatedCurrentSourcePageType = null;

				SUT.Navigated += (s, e) =>
				{
					Assert.AreEqual(navigatedSourcePageType, SUT.SourcePageType);
					Assert.AreEqual(navigatedCurrentSourcePageType, SUT.SourcePageType);
				};

				Assert.IsNull(SUT.SourcePageType);
				Assert.IsNull(SUT.CurrentSourcePageType);

				// Navigate from null to SourceTypePage1

				navigatingSourcePageType = null;
				navigatingCurrentSourcePageType = null;

				navigatedSourcePageType = typeof(SourceTypePage1);
				navigatedCurrentSourcePageType = typeof(SourceTypePage1);

				SUT.Navigate(typeof(SourceTypePage1));

				Assert.AreEqual(typeof(SourceTypePage1), SUT.SourcePageType);
				Assert.AreEqual(typeof(SourceTypePage1), SUT.CurrentSourcePageType);

				// Navigate from SourceTypePage1 to 2

				navigatingSourcePageType = typeof(SourceTypePage1);
				navigatingCurrentSourcePageType = typeof(SourceTypePage1);

				navigatedSourcePageType = typeof(SourceTypePage2);
				navigatedCurrentSourcePageType = typeof(SourceTypePage2);

				SUT.Navigate(typeof(SourceTypePage2));

				Assert.AreEqual(typeof(SourceTypePage2), SUT.SourcePageType);
				Assert.AreEqual(typeof(SourceTypePage2), SUT.CurrentSourcePageType);

				// Navigate back

				navigatingSourcePageType = typeof(SourceTypePage2);
				navigatingCurrentSourcePageType = typeof(SourceTypePage2);

				navigatedSourcePageType = typeof(SourceTypePage1);
				navigatedCurrentSourcePageType = typeof(SourceTypePage1);

				SUT.Navigate(typeof(SourceTypePage1));

				Assert.AreEqual(typeof(SourceTypePage1), SUT.SourcePageType);
				Assert.AreEqual(typeof(SourceTypePage1), SUT.CurrentSourcePageType);
			}
			finally
			{
				SourceTypePage.PageNavigatedFrom -= OnPageNavigatedFrom;
				SourceTypePage.PageNavigatingFrom -= OnPageNavigatingFrom;
				SourceTypePage.PageNavigatedTo -= OnPageNavigatedTo;
			}
		}

		[TestMethod]
		public void When_SourcePageType_Set()
		{
			var SUT = new Frame();
			SUT.SourcePageType = typeof(MyPage);
			Assert.IsInstanceOfType(SUT.Content, typeof(MyPage));
		}

		[TestMethod]
		public void When_SourcePageType_Set_Null()
		{
			var SUT = new Frame();
			SUT.Navigate(typeof(MyPage));
			Assert.ThrowsExactly<ArgumentNullException>(
				() => SUT.SourcePageType = null);
		}

		[TestMethod]
		public void When_Content_Changes_Page()
		{
			var SUT = new Frame();
			SUT.Navigate(typeof(MyPage));

			Assert.AreEqual(typeof(MyPage), SUT.SourcePageType);
			Assert.AreEqual(typeof(MyPage), SUT.CurrentSourcePageType);

			SUT.Content = new FirstPage();

			Assert.AreEqual(typeof(MyPage), SUT.SourcePageType);
			Assert.AreEqual(typeof(MyPage), SUT.CurrentSourcePageType);
		}

		internal static NavigateOrderTracker _navigateOrderTracker = null;

		[TestMethod]
		public void When_NavigatingBetweenPages()
		{
			// We must use a static field, as page is created internally by the frame.
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

	abstract class SourceTypePage : Page
	{
		protected internal override void OnNavigatedFrom(NavigationEventArgs e)
		{
			base.OnNavigatedFrom(e);
			PageNavigatedFrom?.Invoke(this, null);
		}

		protected internal override void OnNavigatedTo(NavigationEventArgs e)
		{
			base.OnNavigatedTo(e);
			PageNavigatedTo?.Invoke(this, null);
		}

		protected internal override void OnNavigatingFrom(NavigatingCancelEventArgs e)
		{
			base.OnNavigatingFrom(e);
			PageNavigatingFrom?.Invoke(this, null);
		}

		public static event EventHandler PageNavigatedFrom;
		public static event EventHandler PageNavigatedTo;
		public static event EventHandler PageNavigatingFrom;
	}

	class SourceTypePage1 : SourceTypePage { }

	class SourceTypePage2 : SourceTypePage { }

	class NavigateOrderTracker
	{
		public bool NavigatedFrom { get; set; }

		public bool NavigatedTo { get; set; }

		public bool FrameNavigated { get; set; }

		public void OnFrameNavigated()
		{
			// Frame.Navigated is the first event
			Assert.IsFalse(FrameNavigated);
			Assert.IsFalse(NavigatedFrom);
			Assert.IsFalse(NavigatedTo);

			FrameNavigated = true;
		}

		public void OnPageNavigatedFrom()
		{
			// Page.NavigatedFrom occurs after frame navigated.
			Assert.IsTrue(FrameNavigated);
			Assert.IsFalse(NavigatedFrom);
			Assert.IsFalse(NavigatedTo);

			NavigatedFrom = true;
		}

		public void OnPageNavigatedTo()
		{
			// Page.NavigatedTo is the last event
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
