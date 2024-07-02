using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;

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

	[TestCleanup]
	public void Cleanup()
	{
		FrameNavigateFirstPage.TestFrame = null;
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
