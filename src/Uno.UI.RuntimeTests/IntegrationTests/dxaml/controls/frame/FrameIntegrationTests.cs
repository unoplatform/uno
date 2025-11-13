using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Tests.Enterprise;
using Private.Infrastructure;

namespace Uno.UI.RuntimeTests.IntegrationTests;

[TestClass]
[RequiresFullWindow]
public class FrameIntegrationTests : BaseDxamlTestClass
{
#if HAS_UNO
	private bool _originalFrameMode;
#endif

	[TestInitialize]
	public void Init()
	{
#if HAS_UNO
		_originalFrameMode = FeatureConfiguration.Frame.UseWinUIBehavior;
		FeatureConfiguration.Frame.UseWinUIBehavior = true;
#endif
	}

	[TestCleanup]
	public void Cleanup()
	{
#if HAS_UNO
		FeatureConfiguration.Frame.UseWinUIBehavior = _originalFrameMode;
#endif
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task CanInstantiate()
	{
		var act = () => new Microsoft.UI.Xaml.Controls.Frame();
		act.Should().NotThrow();
	}

	[TestMethod]
	[RunsOnUIThread]
	public async Task CanEnterAndLeaveLiveTree()
	{
		var frame = new Microsoft.UI.Xaml.Controls.Frame();
		bool unloaded = false;
		frame.Unloaded += (snd, e) => unloaded = true;
		TestServices.WindowHelper.WindowContent = frame;
		await TestServices.WindowHelper.WaitForLoaded(frame);
		TestServices.WindowHelper.WindowContent = null;
		await TestServices.WindowHelper.WaitFor(() => unloaded);
	}

	[TestMethod]
	public async Task CanRaiseNavigationEvents()
	{
		Frame frame = null;
		var frameNavigatingEventRegistration = CreateSafeEventRegistration<Frame, NavigatingCancelEventHandler>("Navigating");
		var frameNavigatedEventRegistration = CreateSafeEventRegistration<Frame, NavigatedEventHandler>("Navigated");
		var frameNavigatingEvent = new Event();
		var frameNavigatedEvent = new Event();

		Type pageType = typeof(Page);

		await TestServices.RunOnUIThread(() =>
		{
			frame = new Frame();
			frameNavigatingEventRegistration.Attach(frame, (s, e) =>
				{
					frameNavigatingEvent.Set();
				});
			frameNavigatedEventRegistration.Attach(frame, (s, e) =>
				{
					frameNavigatedEvent.Set();
				});

			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>
			{
				frame.Navigate(pageType);
			});

		await frameNavigatingEvent.WaitForDefault();
		await frameNavigatedEvent.WaitForDefault();
	}

	[TestMethod]
	public async Task CanNavigateBetweenPages()
	{
		Frame frame = null;
		Type pageType = typeof(Page);

		await TestServices.RunOnUIThread(() =>

		{
			frame = new Frame();
			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			frame.Navigate(pageType, "Page 1");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0);
			frame.Navigate(pageType, "Page 2");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 0);
			frame.Navigate(pageType, "Page 3");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 2, 0);
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 1);
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 2);
			frame.GoForward();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 1);
			frame.GoForward();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 2, 0);
		});
	}

	[TestMethod]
	public async Task CanDisableNavigationHistoryUsingNavigationMethod()
	{
		Frame frame = null;
		Type pageType = typeof(Page);
		Microsoft.UI.Xaml.Navigation.FrameNavigationOptions navOptions = null;

		await TestServices.RunOnUIThread(() =>

		{
			frame = new Frame();
			TestServices.WindowHelper.WindowContent = frame;
			navOptions = new Microsoft.UI.Xaml.Navigation.FrameNavigationOptions();
			navOptions.IsNavigationStackEnabled = false;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			frame.NavigateToType(pageType, "Page 1", navOptions);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0);
			frame.NavigateToType(pageType, "Page 2", navOptions);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0);
			navOptions.IsNavigationStackEnabled = true;
			frame.NavigateToType(pageType, "Page 3", navOptions); // This becomes frame 0
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0);
			frame.NavigateToType(pageType, "Page 2", navOptions); // now it has a back
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 0);
			frame.NavigateToType(pageType, "Page 1", navOptions);

		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 2, 0);
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 1);
		});
	}

	[TestMethod]
	public async Task CanDisableNavigationHistoryFromFrame()
	{
		Frame frame = null;
		Type pageType = typeof(Page);

		await TestServices.RunOnUIThread(() =>

		{
			frame = new Frame();
			frame.IsNavigationStackEnabled = false;
			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			frame.Navigate(pageType, "Page 1");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0);
			frame.Navigate(pageType, "Page 2");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0);
			frame.IsNavigationStackEnabled = true;
			frame.Navigate(pageType, "Page 3"); // This becomes frame 0
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0);
			frame.Navigate(pageType, "Page 1"); // Now it has a back
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 0);
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 1);
		});
	}

	private void ValidateFrameStack(Frame frame, int expectedBackStackDepth, int expectedForwardStackDepth)
	{
		VERIFY_ARE_EQUAL(frame.BackStackDepth, expectedBackStackDepth);
		VERIFY_ARE_EQUAL(frame.ForwardStack.Count, expectedForwardStackDepth);
	}

	[TestMethod]
	public async Task CanNavigateWithNavigationTransitionInfo()
	{
		Frame frame = null;

		PageStackEntry backStackEntry = null;
		PageStackEntry forwardStackEntry = null;
		SlideNavigationTransitionInfo slideNTI = null;
		CommonNavigationTransitionInfo commonNTI = null;

		Type pageType = typeof(Page);

		await TestServices.RunOnUIThread(() =>

		{
			slideNTI = new Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionInfo();
			commonNTI = new Microsoft.UI.Xaml.Media.Animation.CommonNavigationTransitionInfo();

			frame = new Frame();
			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			frame.Navigate(pageType, "Page 1", slideNTI);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0);
			frame.Navigate(pageType, "Page 2", slideNTI);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 0);
			frame.Navigate(pageType, "Page 3", slideNTI);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			LOG_OUTPUT("CanNavigateWithNavigationTransitionInfo: BackStack size=%d", frame.BackStack.Count);
			VERIFY_IS_TRUE(frame.BackStack.Count != 0);
			backStackEntry = frame.BackStack[frame.BackStack.Count - 1];
			VERIFY_IS_TRUE(backStackEntry.NavigationTransitionInfo == slideNTI);

			// Go back to the previous page with CommonNavigationTransitionInfo
			frame.GoBack(commonNTI);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			LOG_OUTPUT("CanNavigateWithNavigationTransitionInfo: ForwardStack size=%d", frame.ForwardStack.Count);
			VERIFY_IS_TRUE(frame.ForwardStack.Count != 0);
			forwardStackEntry = frame.ForwardStack[frame.ForwardStack.Count - 1];
			VERIFY_IS_TRUE(forwardStackEntry.NavigationTransitionInfo == commonNTI);
		});
	}

	[TestMethod]
	public async Task ValidateReEntrancyPrevention()
	{
		Frame frame = null;
		var frameNavigatedEventRegistration = CreateSafeEventRegistration<Frame, NavigatedEventHandler>("Navigated");
		var frameNavigatedEvent = new Event();

		Type pageType = typeof(Page);

		await TestServices.RunOnUIThread(() =>
		{
			frame = new Frame();
			frameNavigatedEventRegistration.Attach(frame, (s, e) =>
			{
				frameNavigatedEvent.Set();
				// This Navigate will be silently suppressed because of the re-entrancy check.
				frame.Navigate(pageType, "Page 2");
			});

			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

			{
				frame.Navigate(pageType, "Page 1");
			});

		await TestServices.WindowHelper.WaitForIdle();
		await frameNavigatedEvent.WaitForDefault();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0);
			frameNavigatedEventRegistration.Detach();
		});

		frameNavigatedEvent.Reset();

		await TestServices.RunOnUIThread(() =>
		{
			frameNavigatedEventRegistration.Attach(frame, (s, e) =>
			{
				frameNavigatedEvent.Set();
				// This navigation will be silently suppressed because of the re-entrancy check.
				frame.GoBack();
			});
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

			{
				frame.Navigate(pageType, "Page 2");
			});

		await TestServices.WindowHelper.WaitForIdle();
		await frameNavigatedEvent.WaitForDefault();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 0);
			frameNavigatedEventRegistration.Detach();
		});

		frameNavigatedEvent.Reset();

		await TestServices.RunOnUIThread(() =>

		{
			frameNavigatedEventRegistration.Attach(frame, (s, e) =>

				{
					frameNavigatedEvent.Set();
					// This navigation will be silently suppressed because of the re-entrancy check.
					frame.GoForward();
				});
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

			{
				frame.GoBack();
			});

		await TestServices.WindowHelper.WaitForIdle();
		await frameNavigatedEvent.WaitForDefault();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 1);
		});
	}

#if HAS_UNO_WINUI // The navigation string tests rely on "Microsoft" namespace, which has different length than "Windows"
	[TestMethod]
	public async Task CanGetNavigationStateWithCurrentPageNull()
	{
		Frame frame = null;

		string navigation = "1,3,2,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 1,0,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 2,0,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 3,0";

		await TestServices.RunOnUIThread(() =>

		{
			frame = new Frame();

			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			frame.SetNavigationState(navigation, true);

			var navigationHistory = frame.GetNavigationState();

			VERIFY_ARE_EQUAL(navigation, navigationHistory);
		});
	}

	[TestMethod]
	public async Task CanSetNavigationStateWithoutNavigatingToCurrent()
	{
		Frame frame = null;
		string navigation1 = "1,3,2,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 1,0,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 2,0,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 3,0";
		string navigation2 = "1,3,1,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 1,0,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 2,0,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 3,0";

		var frameNavigatingEventRegistration = CreateSafeEventRegistration<Frame, NavigatingCancelEventHandler>("Navigating");
		var frameNavigatedEventRegistration = CreateSafeEventRegistration<Frame, NavigatedEventHandler>("Navigated");
		var frameNavigatingEvent = new Event();
		var frameNavigatedEvent = new Event();

		await TestServices.RunOnUIThread(() =>
		{
			frame = new Frame();

			frameNavigatingEventRegistration.Attach(frame, (s, e) =>
				{
					frameNavigatingEvent.Set();
				});
			frameNavigatedEventRegistration.Attach(frame, (s, e) =>
			{
				frameNavigatedEvent.Set();
			});

			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

			{
				frame.SetNavigationState(navigation1, true);
			});

		// Validate SetNavigationState doesn't trigger navigation

		await frameNavigatingEvent.WaitForNoThrow(1000);
		await frameNavigatedEvent.WaitForNoThrow(1000);

		await ValidateGoBackBehaviorWhenCurrentIsNull(frame, navigation2);
		await ValidateGoForwardBehaviorWhenCurrentIsNull(frame, navigation2);
		await ValidateNavigateBehaviorWhenCurrentIsNull(frame, navigation2);
	}
#endif

	private async Task ValidateGoBackBehaviorWhenCurrentIsNull(Frame frame, string navigationHistory)
	{
		// Validate GoBack doesn't add items to the forward stack when current page is null

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 3, 0);
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 2, 0);
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 1);
			frame.SetNavigationState(navigationHistory, true);
		});

		await TestServices.WindowHelper.WaitForIdle();

		// Validate GoBack doesn't add items to the forward stack when current page is null and forward stack is not empty

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 2, 1);
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 1);
		});
	}

	private async Task ValidateGoForwardBehaviorWhenCurrentIsNull(Frame frame, string navigationHistory)
	{
		// Validate GoForward doesn't add items to the back stack when current page is null

		await TestServices.RunOnUIThread(() =>

		{
			frame.SetNavigationState(navigationHistory, true);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 2, 1);
			frame.GoForward();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 2, 0);
		});
	}

	private async Task ValidateNavigateBehaviorWhenCurrentIsNull(Frame frame, string navigationHistory)
	{
		// Validate Navigate works when current page is null and that forward stack is cleared after navigation.

		Type pageType = typeof(Page);

		await TestServices.RunOnUIThread(() =>

		{
			frame.SetNavigationState(navigationHistory, true);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 2, 1);
			frame.Navigate(pageType);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 2, 0);
		});
	}

	[TestMethod]
	public async Task CacheModeDisabled()
	{
		await VerifyCachePageNavigationHelper(NavigationCacheMode.Disabled, new int[7] { 1, 10, 2, 11, 20, 12, 3 });
	}

	[TestMethod]
	public async Task CacheModeEnabled()
	{
		await VerifyCachePageNavigationHelper(NavigationCacheMode.Enabled, new int[7] { 1, 10, 1, 10, 20, 10, 3 });
	}

	[TestMethod]
	public async Task CacheModeRequired()
	{
		await VerifyCachePageNavigationHelper(NavigationCacheMode.Required, new int[7] { 1, 10, 1, 10, 20, 10, 1 });
	}

	[TestMethod]
	public async Task CanceledNavigation()
	{
		Frame frame = null;
		var frameNavigatingEventRegistration = CreateSafeEventRegistration<Frame, NavigatingCancelEventHandler>("Navigating");
		var frameStoppedEventRegistration = CreateSafeEventRegistration<Frame, NavigationStoppedEventHandler>("NavigationStopped");
		var frameNavigatingEvent = new Event();
		var frameStoppedEvent = new Event();

		Type pageType = typeof(Page);

		await TestServices.RunOnUIThread(() =>
		{
			frame = new Frame();
			frameNavigatingEventRegistration.Attach(frame, (s, args) =>
				{
					args.Cancel = true;
					frameNavigatingEvent.Set();
				});
			frameStoppedEventRegistration.Attach(frame, (s, e) =>
			{
				frameStoppedEvent.Set();
			});

			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>
		{
			frame.Navigate(pageType);
		});

		await frameNavigatingEvent.WaitForDefault();
		await frameStoppedEvent.WaitForDefault();
	}

	private async Task VerifyCachePageNavigationHelper(NavigationCacheMode cacheMode, int[] expectedValues)
	{
		Frame frame = null;

		await TestServices.RunOnUIThread(() =>

		{
			frame = new Frame();
			frame.CacheSize = 2;
			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		// When NavigationCacheMode is Disabled all InstanceCounter will pick the latest Counter value
		// When NavigationCacheMode is Required all InstanceCounter will always use the initial Counter value

		await TestServices.RunOnUIThread(() =>

		{
			FirstTestPage.Counter = 1;
			SecondTestPage.Counter = 10;
			ThirdTestPage.Counter = 20;
			FirstTestPage.CacheMode = cacheMode;
			SecondTestPage.CacheMode = cacheMode;
			ThirdTestPage.CacheMode = cacheMode;
			frame.Navigate(typeof(FirstTestPage), "Page 1");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = (FirstTestPage)frame.Content;
			VERIFY_ARE_EQUAL(expectedValues[0], page.InstanceCounter);

			FirstTestPage.Counter = 2;
			frame.Navigate(typeof(SecondTestPage), "Page 2");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = (SecondTestPage)frame.Content;
			VERIFY_ARE_EQUAL(expectedValues[1], page.InstanceCounter);

			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = (FirstTestPage)frame.Content;

			// When NavigationCacheMode is Enabled InstanceCounter will still use the initial Counter of FirstTestPage value as it is within the CacheSize limit
			VERIFY_ARE_EQUAL(expectedValues[2], page.InstanceCounter);

			SecondTestPage.Counter = 11;
			frame.GoForward();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = (SecondTestPage)frame.Content;

			// When NavigationCacheMode is Enabled InstanceCounter will still use the initial Counter value of SecondTestPage as it is within the CacheSize limit
			VERIFY_ARE_EQUAL(expectedValues[3], page.InstanceCounter);

			frame.Navigate(typeof(ThirdTestPage), "Page 3");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = (ThirdTestPage)frame.Content;

			// When NavigationCacheMode is Enabled InstanceCounter will still use the initial Counter value of ThirdTestPage as it is within the CacheSize limit
			VERIFY_ARE_EQUAL(expectedValues[4], page.InstanceCounter);

			SecondTestPage.Counter = 12;
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = (SecondTestPage)frame.Content;

			// When NavigationCacheMode is Enabled InstanceCounter will still use the initial Counter value of SecondTestPage as it is within the CacheSize limit
			VERIFY_ARE_EQUAL(expectedValues[5], page.InstanceCounter);

			FirstTestPage.Counter = 3;
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = (FirstTestPage)frame.Content;

			// When NavigationCacheMode is Enabled this page will be regenerated due to CacheSize and InstanceCounter will use the latest Counter value of FirstTestPage
			VERIFY_ARE_EQUAL(expectedValues[6], page.InstanceCounter);
		});

		await TestServices.WindowHelper.WaitForIdle();
	}
}
