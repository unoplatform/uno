using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Private.Infrastructure;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Uno.Disposables;
using Uno.UI.RuntimeTests.Helpers;
using Combinatorial.MSTest;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
[RequiresFullWindow]
[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeWinUI)]
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
			Assert.AreEqual(typeof(FrameNavigateFirstPage), e.SourcePageType);
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

	private async Task When_Page_Loaded_Navigates_Inner(bool yield)
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

		Assert.HasCount(2, SUT.BackStack);

		SUT.GoBack();

		SUT.IsNavigationStackEnabled = false;

		Assert.HasCount(1, SUT.BackStack);
		Assert.HasCount(1, SUT.ForwardStack);

		SUT.Navigate(typeof(MyPage));
		SUT.Navigate(typeof(MyPage));

		Assert.IsInstanceOfType(SUT.Content, typeof(MyPage));
		Assert.HasCount(1, SUT.BackStack);
		Assert.HasCount(1, SUT.ForwardStack);

		SUT.GoBack();

		Assert.IsInstanceOfType(SUT.Content, typeof(FirstPage));
		Assert.HasCount(1, SUT.BackStack);
		Assert.HasCount(1, SUT.ForwardStack);

		SUT.GoForward();

		Assert.IsInstanceOfType(SUT.Content, typeof(ThirdPage));
		Assert.HasCount(1, SUT.BackStack);
		Assert.HasCount(1, SUT.ForwardStack);
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
		Assert.HasCount(1, SUT.BackStack);
		Assert.HasCount(1, SUT.ForwardStack);

		SUT.IsNavigationStackEnabled = true;
		SUT.GoBack();

		Assert.IsInstanceOfType(SUT.Content, typeof(FirstPage));
		Assert.IsEmpty(SUT.BackStack);
		Assert.HasCount(2, SUT.ForwardStack);
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

	[TestMethod]
	public async Task When_Exception_In_Page_Ctor()
	{
		var SUT = new Frame()
		{
			Width = 200,
			Height = 200
		};

#if HAS_UNO
		bool navigationFailed = false;
		SUT.NavigationFailed += (s, e) => navigationFailed = true;
#endif

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);

		var exception = Assert.ThrowsExactly<NotSupportedException>(() => SUT.Navigate(typeof(ExceptionInCtorPage)));
		Assert.AreEqual("Crashed", exception.Message);
#if HAS_UNO
		Assert.IsFalse(navigationFailed);
#endif
	}

	[TestMethod]
	public async Task When_Exception_In_OnNavigatedTo()
	{
		var SUT = new Frame()
		{
			Width = 200,
			Height = 200
		};

		bool navigationFailed = false;
		SUT.NavigationFailed += (s, e) => navigationFailed = true;

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);

		SUT.Navigate(typeof(MyPage));
		var previousContent = SUT.Content;

		var exception = Assert.ThrowsExactly<NotSupportedException>(() => SUT.Navigate(typeof(ExceptionInOnNavigatedToPage)));
		Assert.AreEqual("Crashed", exception.Message);
		Assert.IsTrue(navigationFailed);
		Assert.AreSame(previousContent, SUT.Content);
	}

	[TestMethod]
	public async Task When_Exception_In_OnNavigatedTo_And_NavigationFailed_Handled()
	{
		var SUT = new Frame()
		{
			Width = 200,
			Height = 200
		};

		bool navigationFailed = false;
		SUT.NavigationFailed += (s, e) =>
		{
			navigationFailed = true;
			e.Handled = true;
		};

		try
		{
			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForLoaded(SUT);

			SUT.Navigate(typeof(MyPage));
			var previousContent = SUT.Content;

			// A handled NavigationFailed lets the app continue: no exception, previous content restored.
			var result = SUT.Navigate(typeof(ExceptionInOnNavigatedToPage));

			Assert.IsTrue(navigationFailed);
			Assert.IsTrue(result);
			Assert.AreSame(previousContent, SUT.Content);
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_BackStack_Then_Go_Back()
	{
		var SUT = new Frame()
		{
			Width = 200,
			Height = 200
		};
		var navigated = false;

		SUT.Navigated += (s, e) =>
		{
			navigated = true;
		};

		TestServices.WindowHelper.WindowContent = SUT;
		await TestServices.WindowHelper.WaitForLoaded(SUT);
		SUT.Navigate(typeof(FirstPage));
		await TestServices.WindowHelper.WaitFor(() => navigated);
		navigated = false;

		Assert.IsInstanceOfType(SUT.Content, typeof(FirstPage));

		SUT.BackStack.Add(new PageStackEntry(typeof(SecondPage), null, new SuppressNavigationTransitionInfo()));
		SUT.GoBack();

		await TestServices.WindowHelper.WaitFor(() => navigated);
		Assert.IsInstanceOfType(SUT.Content, typeof(SecondPage));
	}

	[TestMethod]
	public async Task When_Entrance_Transition_First_Frame_Of_New_Page_Is_Hidden()
	{
#if __SKIA__
		var frame = new Frame { Width = 300, Height = 200, ContentTransitions = new TransitionCollection { new NavigationThemeTransition() } };
		var button = new Button { Content = "Navigate" };
		button.Click += (_, _) => frame.Navigate(typeof(SecondPage), null, new EntranceNavigationTransitionInfo());
		TestServices.WindowHelper.WindowContent = new StackPanel { Children = { button, frame } };
		await TestServices.WindowHelper.WaitForLoaded(button);

		frame.Navigate(typeof(FirstPage));
		await TestServices.WindowHelper.WaitFor(() => frame.Content is FirstPage { IsLoaded: true });
		await TestServices.WindowHelper.WaitForIdle();

		double? firstOpacity = null;
		var compositionTarget = (Microsoft.UI.Xaml.Media.CompositionTarget)frame.Visual.CompositionTarget!;
		Action onFrameRendered = () =>
		{
			if (firstOpacity is null && frame.Content is SecondPage page)
			{
				firstOpacity = page.Opacity;
			}
		};

		var injector = global::Windows.UI.Input.Preview.Injection.InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
		using var mouse = global::Uno.UI.DevTools.Input.InputInjectorExtensions.GetMouse(injector);
		var buttonBounds = button.GetAbsoluteBounds();
		var center = new global::Windows.Foundation.Point(buttonBounds.X + buttonBounds.Width / 2, buttonBounds.Y + buttonBounds.Height / 2);
		mouse.MoveTo(center);

		compositionTarget.FrameRendered += onFrameRendered;
		try
		{
			mouse.Press(center);
			mouse.Release();
			await TestServices.WindowHelper.WaitFor(() => firstOpacity is not null);
		}
		finally
		{
			compositionTarget.FrameRendered -= onFrameRendered;
		}

		// Entrance holds the incoming page at opacity 0 before fading it in; a fully opaque first frame is the flash.
		Assert.AreEqual(0, firstOpacity!.Value, 0.01, "The first frame showing the new page should still have it hidden.");
#else
		await Task.CompletedTask;
		Assert.Inconclusive("Inspects Uno's recorded frames.");
#endif
	}

	#region Navigation transition orchestration

	[TestMethod]
	public async Task When_NavigationThemeTransition_Unload_Uses_New_Page_Transitions()
	{
		var frame = await CreateTransitionFrame(new NavigationThemeTransition { DefaultNavigationTransitionInfo = new EntranceNavigationTransitionInfo() });

		frame.Navigate(typeof(FirstPage));
		await WaitForTransitionsToSettle(frame);
		var oldPage = (Page)frame.Content;

		// The new page asks for no animation, which WinUI also applies to the page navigated away from.
		using var oldPageOpacity = MinOpacityTracker.Start(oldPage);
		frame.Navigate(typeof(SuppressTransitionPage));
		await WaitForTransitionsToSettle(frame);

		Assert.AreEqual(1.0, oldPageOpacity.MinOpacity, 0.001, "The outgoing page should use the new page's SuppressNavigationTransitionInfo.");
	}

	[TestMethod]
	public async Task When_Page_Transitions_Has_NavigationThemeTransition()
	{
		var frame = await CreateTransitionFrame(null);

		// The initial navigation only animates when a NavigationThemeTransition is found on the page or the frame.
		using var pageOpacity = new MinOpacityTracker();
		frame.Navigate(typeof(NavigationThemeTransitionPage));
		pageOpacity.Track((UIElement)frame.Content);
		await WaitForTransitionsToSettle(frame);

		Assert.IsTrue(pageOpacity.MinOpacity < 0.5, $"Page.Transitions' NavigationThemeTransition should animate the page in (min opacity {pageOpacity.MinOpacity}).");
	}

	[TestMethod]
	public async Task When_Navigating_Default_Transition_Info_Stored_On_Committed_Entry()
	{
		var frame = await CreateTransitionFrame(new NavigationThemeTransition { DefaultNavigationTransitionInfo = new DrillInNavigationTransitionInfo() });

		var firstInfo = new EntranceNavigationTransitionInfo();
		frame.Navigate(typeof(FirstPage), null, firstInfo);
		await WaitForTransitionsToSettle(frame);

		frame.Navigate(typeof(SecondPage));
		await WaitForTransitionsToSettle(frame);

		// WinUI stores the transition's default on the current entry once the navigation is committed,
		// so the entry of the page navigated away from keeps its own info.
		Assert.AreEqual(1, frame.BackStack.Count);
		Assert.AreSame(firstInfo, frame.BackStack[0].NavigationTransitionInfo);
	}

	private static async Task<Frame> CreateTransitionFrame(NavigationThemeTransition contentTransition)
	{
		var frame = new Frame { Width = 300, Height = 200 };
		if (contentTransition is not null)
		{
			frame.ContentTransitions = new TransitionCollection { contentTransition };
		}

		TestServices.WindowHelper.WindowContent = frame;
		await TestServices.WindowHelper.WaitForLoaded(frame);
		return frame;
	}

	// The longest built-in navigation storyboard (DrillIn) runs for under 800ms.
	private static async Task WaitForTransitionsToSettle(Frame frame)
	{
		await TestServices.WindowHelper.WaitFor(() => frame.Content is FrameworkElement { IsLoaded: true });
		await Task.Delay(1000);
		await TestServices.WindowHelper.WaitForIdle();
	}

	private sealed class MinOpacityTracker : IDisposable
	{
		private UIElement _element;

		public double MinOpacity { get; private set; } = double.MaxValue;

		public static MinOpacityTracker Start(UIElement element)
		{
			var tracker = new MinOpacityTracker();
			tracker.Track(element);
			return tracker;
		}

		public void Track(UIElement element)
		{
			_element = element;
			MinOpacity = Math.Min(MinOpacity, element.Opacity);
			CompositionTarget.Rendering += OnRendering;
		}

		private void OnRendering(object sender, object e) => MinOpacity = Math.Min(MinOpacity, _element.Opacity);

		public void Dispose() => CompositionTarget.Rendering -= OnRendering;
	}

	#endregion

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

internal partial class NavigationThemeTransitionPage : Page
{
	public NavigationThemeTransitionPage()
	{
		Transitions = new TransitionCollection { new NavigationThemeTransition() };
	}
}

internal partial class SuppressTransitionPage : Page
{
	public SuppressTransitionPage()
	{
		Transitions = new TransitionCollection { new NavigationThemeTransition { DefaultNavigationTransitionInfo = new SuppressNavigationTransitionInfo() } };
	}
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

	private async void FrameNavigateFirstPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
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

public partial class ExceptionInCtorPage : Page
{
	public ExceptionInCtorPage()
	{
		throw new NotSupportedException("Crashed");
	}
}

public partial class ExceptionInOnNavigatedToPage : Page
{
	public ExceptionInOnNavigatedToPage()
	{
	}

	protected
#if HAS_UNO
	internal
#endif
	override void OnNavigatedTo(NavigationEventArgs e)
	{
		base.OnNavigatedTo(e);
		throw new NotSupportedException("Crashed");
	}
}
