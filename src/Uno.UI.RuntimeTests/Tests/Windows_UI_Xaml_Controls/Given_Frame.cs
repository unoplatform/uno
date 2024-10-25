using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;
using Windows.UI.Xaml.Navigation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[RequiresFullWindow]
public class Given_Frame
{
	[TestMethod]
	public async Task When_Page_Ctor_Navigates()
	{
		var frame = new Frame();
		TestServices.WindowHelper.WindowContent = frame;
		await TestServices.WindowHelper.WaitForLoaded(frame);

		FrameNavigateFirstPage.TestFrame = frame;
		FrameNavigateFirstPage.NavigateInCtor = true;

		var navigating = false;
		var navigated = false;
		frame.Navigating += (snd, e) =>
		{
			Assert.IsFalse(navigating);
			Assert.IsFalse(navigated);
			navigating = true;
			Assert.AreEqual(e.SourcePageType, typeof(FrameNavigateFirstPage));
		};
		frame.Navigated += (snd, e) =>
		{
			Assert.IsTrue(navigating);
			Assert.IsFalse(navigated);
			navigated = true;
		};
		frame.NavigationFailed += (snd, e) =>
		{
			Assert.Fail($"Navigation failed: {e.Exception}");
		};
		frame.Navigate(typeof(FrameNavigateFirstPage));
		await TestServices.WindowHelper.WaitFor(() => navigated);
		Assert.IsInstanceOfType(frame.Content, typeof(FrameNavigateFirstPage));
	}

	[TestMethod]
#if !UNO_HAS_ENHANCED_LIFECYCLE
	[Ignore("This test fails on Uno Platform targets. See https://github.com/unoplatform/uno/issues/14300")]
#endif
	public Task When_Page_Loaded_Navigates_Without_Yield() =>
		When_Page_Loaded_Navigates_Inner(false);

	[TestMethod]
	public Task When_Page_Loaded_Navigates_With_Yield() =>
		When_Page_Loaded_Navigates_Inner(true);

	public async Task When_Page_Loaded_Navigates_Inner(bool yield)
	{
		var frame = new Frame();
		TestServices.WindowHelper.WindowContent = frame;
		await TestServices.WindowHelper.WaitForLoaded(frame);

		FrameNavigateFirstPage.TestFrame = frame;
		FrameNavigateFirstPage.NavigateInCtor = false;
		FrameNavigateFirstPage.Yield = yield;

		var navigatingFirstPage = false;
		var navigatingSecondPage = false;
		var navigatedFirstPage = false;
		var navigatedSecondPage = false;
		frame.Navigating += (snd, e) =>
		{
			if (e.SourcePageType == typeof(FrameNavigateFirstPage))
			{
				Assert.IsFalse(navigatingFirstPage);
				Assert.IsFalse(navigatedFirstPage);
				Assert.IsFalse(navigatingSecondPage);
				Assert.IsFalse(navigatedSecondPage);
				navigatingFirstPage = true;
			}
			else if (e.SourcePageType == typeof(FrameNavigateSecondPage))
			{
				Assert.IsTrue(navigatingFirstPage);
				Assert.IsTrue(navigatedFirstPage);
				Assert.IsFalse(navigatingSecondPage);
				Assert.IsFalse(navigatedSecondPage);
				navigatingSecondPage = true;
			}
			else
			{
				Assert.Fail($"Unexpected page type: {e.SourcePageType}");
			}
		};
		frame.Navigated += (snd, e) =>
		{
			if (e.SourcePageType == typeof(FrameNavigateFirstPage))
			{
				Assert.IsTrue(navigatingFirstPage);
				Assert.IsFalse(navigatedFirstPage);
				Assert.IsFalse(navigatingSecondPage);
				Assert.IsFalse(navigatedSecondPage);
				navigatedFirstPage = true;
			}
			else if (e.SourcePageType == typeof(FrameNavigateSecondPage))
			{
				Assert.IsTrue(navigatingFirstPage);
				Assert.IsTrue(navigatedFirstPage);
				Assert.IsTrue(navigatingSecondPage);
				Assert.IsFalse(navigatedSecondPage);
				navigatedSecondPage = true;
			}
			else
			{
				Assert.Fail($"Unexpected page type: {e.SourcePageType}");
			}
		};
		frame.NavigationFailed += (snd, e) =>
		{
			Assert.Fail($"Navigation failed: {e.Exception}");
		};

		frame.Navigate(typeof(FrameNavigateFirstPage));
		await TestServices.WindowHelper.WaitFor(() => navigatedSecondPage);
		Assert.IsInstanceOfType(frame.Content, typeof(FrameNavigateSecondPage));
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

#if !__SKIA__ // This test only applies to legacy frame which keeps all pages in memory
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
#endif

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

			Assert.AreEqual(null, SUT.SourcePageType);
			Assert.AreEqual(null, SUT.CurrentSourcePageType);

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
		Assert.ThrowsException<ArgumentNullException>(
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

	[TestCleanup]
	public void Cleanup()
	{
		FrameNavigateFirstPage.TestFrame = null;
	}
}


internal partial class MyPage : Page
{
}

internal partial class FirstPage : Page
{
}

internal partial class SecondPage : Page
{
}

internal partial class ThirdPage : Page
{
}

internal partial class DisallowNavigatingFromPage : Page
{
	public static bool NavigatingFromCalled = false;

	protected
#if HAS_UNO
	internal
#endif
	override void OnNavigatingFrom(NavigatingCancelEventArgs e)
	{
		NavigatingFromCalled = true;
	}
}

internal abstract partial class SourceTypePage : Page
{
	protected
#if HAS_UNO
	internal
#endif
	override void OnNavigatedFrom(NavigationEventArgs e)
	{
		base.OnNavigatedFrom(e);
		PageNavigatedFrom?.Invoke(this, null);
	}

	protected
#if HAS_UNO
	internal
#endif
	override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		PageNavigatedTo?.Invoke(this, null);
	}

	protected
#if HAS_UNO
	internal
#endif
	override void OnNavigatingFrom(NavigatingCancelEventArgs e)
	{
		base.OnNavigatingFrom(e);
		PageNavigatingFrom?.Invoke(this, null);
	}

	public static event EventHandler PageNavigatedFrom;
	public static event EventHandler PageNavigatedTo;
	public static event EventHandler PageNavigatingFrom;
}

internal partial class SourceTypePage1 : SourceTypePage { }

internal partial class SourceTypePage2 : SourceTypePage { }

internal class NavigateOrderTracker
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

internal partial class NavigationTrackingPage : Page
{
	protected
#if HAS_UNO
	internal
#endif
	override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		Given_Frame._navigateOrderTracker?.OnPageNavigatedTo();
	}

	protected
#if HAS_UNO
	internal
#endif
	override void OnNavigatedFrom(NavigationEventArgs e)
	{
		base.OnNavigatedFrom(e);
		Given_Frame._navigateOrderTracker?.OnPageNavigatedFrom();
	}
}

public partial class FrameNavigateFirstPage : Page
{
	internal static Frame TestFrame { get; set; }

	internal static bool NavigateInCtor { get; set; }

	internal static bool Yield { get; set; }

	public FrameNavigateFirstPage()
	{
		if (NavigateInCtor)
		{
			TestFrame.Navigate(typeof(FrameNavigateSecondPage));
		}

		Loaded += FrameNavigateFirstPage_Loaded;
	}

	private async void FrameNavigateFirstPage_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
	{
		if (!NavigateInCtor)
		{
			if (Yield)
			{
				await Task.Yield();
			}

			TestFrame.Navigate(typeof(FrameNavigateSecondPage));
		}
	}
}

public partial class FrameNavigateSecondPage : Page
{
}
