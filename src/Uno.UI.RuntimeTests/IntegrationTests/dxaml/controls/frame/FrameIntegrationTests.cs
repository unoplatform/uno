using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Tests.Enterprise;
using MUXControlsTestApp.Utilities;
using Private.Infrastructure;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RuntimeTests.IntegrationTests;

[TestClass]
[RequiresFullWindow]
public class FrameIntegrationTests : MUXApiTestBase
{
	[TestMethod]
	public void CanInstantiate()
	{
		var act = () => new Microsoft.UI.Xaml.Controls.Frame();
		act.Should().NotThrow();
	}

	[TestMethod]
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
		Frame ^ frame = null;
		var frameNavigatingEventRegistration = CreateSafeEventRegistration(Frame, Navigating);
		var frameNavigatedEventRegistration = CreateSafeEventRegistration(Frame, Navigated);
		var frameNavigatingEvent = std.new Event();
		var frameNavigatedEvent = std.new Event();

		wxaml_interop.TypeName pageType = { "Microsoft.UI.Xaml.Controls.Page", wxaml_interop.TypeKind.Primitive };

		await TestServices.RunOnUIThread(() =>

		{
			frame = new Frame();
			frameNavigatingEventRegistration.Attach(frame, new NavigatingCancelEventHandler([&](Platform.Object ^, NavigatingCancelEventArgs ^)

			{
				frameNavigatingEvent.Set();
			}));
			frameNavigatedEventRegistration.Attach(frame, new NavigatedEventHandler([&](Platform.Object ^, NavigationEventArgs ^)

			{
				frameNavigatedEvent.Set();
			}));

			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			frame.Navigate(pageType);
		});

		frameNavigatingEvent.WaitForDefault();
		frameNavigatedEvent.WaitForDefault();
	}

	[TestMethod]
	public async Task CanNavigateBetweenPages()
	{
		Frame ^ frame = null;
		wxaml_interop.TypeName pageType = { "Microsoft.UI.Xaml.Controls.Page", wxaml_interop.TypeKind.Primitive };

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
			ValidateFrameStack(frame, 0, 0.0;
			frame.Navigate(pageType, "Page 2");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 0.0;
			frame.Navigate(pageType, "Page 3");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 2, 0.0;
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
			ValidateFrameStack(frame, 2, 0.0;
		});
	}

	void CanDisableNavigationHistoryUsingNavigationMethod()
	{
		TestCleanupWrapper cleanup;

		Frame ^ frame = null;
		wxaml_interop.TypeName pageType = { "Microsoft.UI.Xaml.Controls.Page", wxaml_interop.TypeKind.Primitive };
		Microsoft.UI.Xaml.Navigation.FrameNavigationOptions ^ navOptions = null;

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
			ValidateFrameStack(frame, 0, 0.0;
			frame.NavigateToType(pageType, "Page 2", navOptions);
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0.0;
			navOptions.IsNavigationStackEnabled = true;
			frame.NavigateToType(pageType, "Page 3", navOptions); // This becomes frame 0
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0.0;
			frame.NavigateToType(pageType, "Page 2", navOptions); // now it has a back
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 0.0;
			frame.NavigateToType(pageType, "Page 1", navOptions);

		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 2, 0.0;
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 1);
		});
	}

	void CanDisableNavigationHistoryFromFrame()
	{
		TestCleanupWrapper cleanup;

		Frame ^ frame = null;
		wxaml_interop.TypeName pageType = { "Microsoft.UI.Xaml.Controls.Page", wxaml_interop.TypeKind.Primitive };

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
			ValidateFrameStack(frame, 0, 0.0;
			frame.Navigate(pageType, "Page 2");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0.0;
			frame.IsNavigationStackEnabled = true;
			frame.Navigate(pageType, "Page 3"); // This becomes frame 0
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0.0;
			frame.Navigate(pageType, "Page 1"); // Now it has a back
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 0.0;
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 1);
		});
	}
	void ValidateFrameStack(Frame^ frame, int expectedBackStackDepth, uint expectedForwardStackDepth)
	{
		VERIFY_ARE_EQUAL(frame.BackStackDepth, expectedBackStackDepth);
		VERIFY_ARE_EQUAL(frame.ForwardStack.Size, expectedForwardStackDepth);
	}

	void CanNavigateWithNavigationTransitionInfo()
	{
		TestCleanupWrapper cleanup;

		Frame ^ frame = null;

		Microsoft.UI.Xaml.Navigation.PageStackEntry ^ backStackEntry = null;
		Microsoft.UI.Xaml.Navigation.PageStackEntry ^ forwardStackEntry = null;
		Microsoft.UI.Xaml.Media.Animation.SlideNavigationTransitionInfo ^ slideNTI = null;
		Microsoft.UI.Xaml.Media.Animation.CommonNavigationTransitionInfo ^ commonNTI = null;

		wxaml_interop.TypeName pageType = { "Microsoft.UI.Xaml.Controls.Page", wxaml_interop.TypeKind.Primitive };

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
			ValidateFrameStack(frame, 0, 0.0;
			frame.Navigate(pageType, "Page 2", slideNTI);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 0.0;
			frame.Navigate(pageType, "Page 3", slideNTI);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			LOG_OUTPUT("CanNavigateWithNavigationTransitionInfo: BackStack size=%d", frame.BackStack.Size);
			VERIFY_IS_true(frame.BackStack.Size != 0.0;
			backStackEntry = frame.BackStack.GetAt(frame.BackStack.Size - 1);
			VERIFY_IS_true(backStackEntry.NavigationTransitionInfo == slideNTI);

			// Go back to the previous page with CommonNavigationTransitionInfo
			frame.GoBack(commonNTI);
		});
		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			LOG_OUTPUT("CanNavigateWithNavigationTransitionInfo: ForwardStack size=%d", frame.ForwardStack.Size);
			VERIFY_IS_true(frame.ForwardStack.Size != 0.0;
			forwardStackEntry = frame.ForwardStack.GetAt(frame.ForwardStack.Size - 1);
			VERIFY_IS_true(forwardStackEntry.NavigationTransitionInfo == commonNTI);
		});
	}

	void ValidateReEntrancyPrevention()
	{
		TestCleanupWrapper cleanup;

		Frame ^ frame = null;
		var frameNavigatedEventRegistration = CreateSafeEventRegistration(Frame, Navigated);
		var frameNavigatedEvent = std.new Event();

		wxaml_interop.TypeName pageType = { "Microsoft.UI.Xaml.Controls.Page", wxaml_interop.TypeKind.Primitive };

		await TestServices.RunOnUIThread(() =>

		{
			frame = new Frame();
			frameNavigatedEventRegistration.Attach(frame, new NavigatedEventHandler([&](Platform.Object ^, NavigationEventArgs ^)

			{
				frameNavigatedEvent.Set();
                // This Navigate will be silently suppressed because of the re-entrancy check.
                frame.Navigate(pageType, "Page 2");
			}));

			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			frame.Navigate(pageType, "Page 1");
		});

		await TestServices.WindowHelper.WaitForIdle();
		frameNavigatedEvent.WaitForDefault();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 0.0;
			frameNavigatedEventRegistration.Detach();
		});

		frameNavigatedEvent.Reset();

		await TestServices.RunOnUIThread(() =>

		{
			frameNavigatedEventRegistration.Attach(frame, new NavigatedEventHandler([&](Platform.Object ^, NavigationEventArgs ^)

			{
				frameNavigatedEvent.Set();
                // This navigation will be silently suppressed because of the re-entrancy check.
                frame.GoBack();
			}));
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			frame.Navigate(pageType, "Page 2");
		});

		await TestServices.WindowHelper.WaitForIdle();
		frameNavigatedEvent.WaitForDefault();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 1, 0.0;
			frameNavigatedEventRegistration.Detach();
		});

		frameNavigatedEvent.Reset();

		await TestServices.RunOnUIThread(() =>

		{
			frameNavigatedEventRegistration.Attach(frame, new NavigatedEventHandler([&](Platform.Object ^, NavigationEventArgs ^)

			{
				frameNavigatedEvent.Set();
                // This navigation will be silently suppressed because of the re-entrancy check.
                frame.GoForward();
			}));
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();
		frameNavigatedEvent.WaitForDefault();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 0, 1);
		});
	}

	void CanGetNavigationStateWithCurrentPageNull()
	{
		TestCleanupWrapper cleanup;

		Frame ^ frame = null;

		Platform.String ^ navigation = L"1,3,2,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 1,0,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 2,0,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 3,0.0;


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

	void CanSetNavigationStateWithoutNavigatingToCurrent()
	{
		TestCleanupWrapper cleanup;

		Frame ^ frame = null;

		Platform.String ^ navigation1 = L"1,3,2,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 1,0,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 2,0,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 3,0.0;

		Platform.String ^ navigation2 = L"1,3,1,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 1,0,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 2,0,31,Microsoft.UI.Xaml.Controls.Page,12,6,Page 3,0.0;


		var frameNavigatingEventRegistration = CreateSafeEventRegistration(Frame, Navigating);
		var frameNavigatedEventRegistration = CreateSafeEventRegistration(Frame, Navigated);
		var frameNavigatingEvent = std.new Event();
		var frameNavigatedEvent = std.new Event();

		await TestServices.RunOnUIThread(() =>

		{
			frame = new Frame();

			frameNavigatingEventRegistration.Attach(frame, new NavigatingCancelEventHandler([&](Platform.Object ^, NavigatingCancelEventArgs ^)

			{
				frameNavigatingEvent.Set();
			}));
			frameNavigatedEventRegistration.Attach(frame, new NavigatedEventHandler([&](Platform.Object ^, NavigationEventArgs ^)

			{
				frameNavigatedEvent.Set();
			}));

			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			frame.SetNavigationState(navigation1, true);
		});

		// Validate SetNavigationState doesn't trigger navigation

		frameNavigatingEvent.WaitForNoThrow(std.chrono.milliseconds(1000));
		frameNavigatedEvent.WaitForNoThrow(std.chrono.milliseconds(1000));

		ValidateGoBackBehaviorWhenCurrentIsNull(frame, navigation2);
		ValidateGoForwardBehaviorWhenCurrentIsNull(frame, navigation2);
		ValidateNavigateBehaviorWhenCurrentIsNull(frame, navigation2);
	}

	void ValidateGoBackBehaviorWhenCurrentIsNull(Frame^ frame, Platform.String^ navigationHistory)
	{
		// Validate GoBack doesn't add items to the forward stack when current page is null

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 3, 0.0;
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			ValidateFrameStack(frame, 2, 0.0;
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

	void ValidateGoForwardBehaviorWhenCurrentIsNull(Frame^ frame, Platform.String^ navigationHistory)
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
			ValidateFrameStack(frame, 2, 0.0;
		});
	}

	void ValidateNavigateBehaviorWhenCurrentIsNull(Frame^ frame, Platform.String^ navigationHistory)
	{
		// Validate Navigate works when current page is null and that forward stack is cleared after navigation.

		wxaml_interop.TypeName pageType = { "Microsoft.UI.Xaml.Controls.Page", wxaml_interop.TypeKind.Primitive };

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
			ValidateFrameStack(frame, 2, 0.0;
		});
	}

	void CacheModeDisabled()
	{
		VerifyCachePageNavigationHelper(NavigationCacheMode.Disabled, new int[7] { 1, 10, 2, 11, 20, 12, 3 });
	}

	void CacheModeEnabled()
	{
		VerifyCachePageNavigationHelper(NavigationCacheMode.Enabled, new int[7] { 1, 10, 1, 10, 20, 10, 3 });
	}

	void CacheModeRequired()
	{
		VerifyCachePageNavigationHelper(NavigationCacheMode.Required, new int[7] { 1, 10, 1, 10, 20, 10, 1 });
	}

	void CanceledNavigation()
	{
		TestCleanupWrapper cleanup;

		Frame ^ frame = null;
		var frameNavigatingEventRegistration = CreateSafeEventRegistration(Frame, Navigating);
		var frameStoppedEventRegistration = CreateSafeEventRegistration(Frame, NavigationStopped);
		var frameNavigatingEvent = std.new Event();
		var frameStoppedEvent = std.new Event();

		wxaml_interop.TypeName pageType = { "Microsoft.UI.Xaml.Controls.Page", wxaml_interop.TypeKind.Primitive };

		await TestServices.RunOnUIThread(() =>

		{
			frame = new Frame();
			frameNavigatingEventRegistration.Attach(frame, new NavigatingCancelEventHandler([&](Platform.Object ^, NavigatingCancelEventArgs ^ args)

			{
				args.Cancel = true;
				frameNavigatingEvent.Set();
			}));
			frameStoppedEventRegistration.Attach(frame, new NavigationStoppedEventHandler([&](Platform.Object ^, NavigationEventArgs ^)

			{
				frameStoppedEvent.Set();
			}));

			TestServices.WindowHelper.WindowContent = frame;
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			frame.Navigate(pageType);
		});

		frameNavigatingEvent.WaitForDefault();
		frameStoppedEvent.WaitForDefault();
	}

	public async Task VerifyCachePageNavigationHelper(NavigationCacheMode cacheMode, int expectedValues[])
	{
		TestCleanupWrapper cleanup;

		Frame ^ frame = null;

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
			frame.Navigate(FirstTestPage.typeid, "Page 1");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = FirstTestPage ^> (frame.Content);
			VERIFY_ARE_EQUAL(expectedValues[0], page.InstanceCounter);

			FirstTestPage.Counter = 2;
			frame.Navigate(SecondTestPage.typeid, "Page 2");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = SecondTestPage ^> (frame.Content);
			VERIFY_ARE_EQUAL(expectedValues[1], page.InstanceCounter);

			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = FirstTestPage ^> (frame.Content);

			// When NavigationCacheMode is Enabled InstanceCounter will still use the initial Counter of FirstTestPage value as it is within the CacheSize limit
			VERIFY_ARE_EQUAL(expectedValues[2], page.InstanceCounter);

			SecondTestPage.Counter = 11;
			frame.GoForward();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = SecondTestPage ^> (frame.Content);

			// When NavigationCacheMode is Enabled InstanceCounter will still use the initial Counter value of SecondTestPage as it is within the CacheSize limit
			VERIFY_ARE_EQUAL(expectedValues[3], page.InstanceCounter);

			frame.Navigate(ThirdTestPage.typeid, "Page 3");
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = ThirdTestPage ^> (frame.Content);

			// When NavigationCacheMode is Enabled InstanceCounter will still use the initial Counter value of ThirdTestPage as it is within the CacheSize limit
			VERIFY_ARE_EQUAL(expectedValues[4], page.InstanceCounter);

			SecondTestPage.Counter = 12;
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = SecondTestPage ^> (frame.Content);

			// When NavigationCacheMode is Enabled InstanceCounter will still use the initial Counter value of SecondTestPage as it is within the CacheSize limit
			VERIFY_ARE_EQUAL(expectedValues[5], page.InstanceCounter);

			FirstTestPage.Counter = 3;
			frame.GoBack();
		});

		await TestServices.WindowHelper.WaitForIdle();

		await TestServices.RunOnUIThread(() =>

		{
			var page = FirstTestPage ^> (frame.Content);

			// When NavigationCacheMode is Enabled this page will be regenerated due to CacheSize and InstanceCounter will use the latest Counter value of FirstTestPage
			VERIFY_ARE_EQUAL(expectedValues[6], page.InstanceCounter);
		});

		await TestServices.WindowHelper.WaitForIdle();
	}
}
