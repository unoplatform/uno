#if __IOS__ || __MACOS__ || __ANDROID__
#define HAS_NATIVE_VIEWS
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml;
#if __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI.Extensions;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_Native_View
	{
		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282! epic")]
#endif
		public async Task When_Added_In_Xaml()
		{
			var page = new NativeView_Page();

			TestServices.WindowHelper.WindowContent = page;
			await TestServices.WindowHelper.WaitForIdle();

			var nativeInPanel = page.hostPanel.FindFirstChild<NativeView>();
			Assert.IsNotNull(nativeInPanel);

			var nativeInBorder = page.hostBorder.FindFirstChild<NativeView>();
			Assert.IsNotNull(nativeInBorder);

			var nativeInButton = page.hostButton.FindFirstChild<NativeView>();
			Assert.IsNotNull(nativeInButton);

			var nativeInSplitViewPane = page.hostSplitView.Pane?.FindFirstChild<NativeView>();
			Assert.IsNotNull(nativeInSplitViewPane);

			var nativeInSplitViewContent = page.hostSplitView.Content?.FindFirstChild<NativeView>();
			Assert.IsNotNull(nativeInSplitViewContent);

			page.hostPopup.IsOpen = true;

			await TestServices.WindowHelper.WaitForIdle();
			var nativeInPopup = page.hostPopup.Child?.FindFirstChild<NativeView>();
			Assert.IsNotNull(nativeInPopup);

			page.hostPopup.IsOpen = false;

			await TestServices.WindowHelper.WaitForIdle();

			page.flyoutHostButton.Flyout.ShowAt(page.flyoutHostButton);

			await TestServices.WindowHelper.WaitForIdle();

			var nativeInFlyout = (page.flyoutHostButton.Flyout as Flyout).Content?.FindFirstChild<NativeView>();
			Assert.IsNotNull(nativeInFlyout);
			page.flyoutHostButton.Flyout.Hide();
			TestServices.WindowHelper.WindowContent = null;
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Inner_IFrameworkElement()
		{
			var page = new NativeView_Page();

			TestServices.WindowHelper.WindowContent = page;
			await TestServices.WindowHelper.WaitForIdle();

			// Validates that the templated parent of the native child is the immediate parent's one, not the one from
			// the outer parent.
			var nativeViewIFrameworkElement01 = page.innerFrameworkElement.FindFirstChild<NativeViewIFrameworkElement>();
			Assert.IsNotNull(nativeViewIFrameworkElement01);
			Assert.AreEqual(page.innerFrameworkElement.Tag, nativeViewIFrameworkElement01.MyValue);
		}

		[TestMethod]
		[RunsOnUIThread]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task When_Grid_Properties_Set()
		{
			var page = new NativeView_Grid_Page();

			TestServices.WindowHelper.WindowContent = page;
			await TestServices.WindowHelper.WaitForLoaded(page);

			var wrapper = (FrameworkElement)page.HostGrid.Children[1];

			await TestServices.WindowHelper.WaitForLoaded(wrapper); // Needed on *sigh* iOS

#if HAS_NATIVE_VIEWS
			Assert.IsInstanceOfType(wrapper, typeof(ContentPresenter));
#endif
			Assert.AreEqual(1, Grid.GetColumn(wrapper));
#if !__ANDROID__ // LayoutSlot currently wrongly returns an offset of (0,0) on Android
			var slot = LayoutInformation.GetLayoutSlot(wrapper);
			Assert.AreEqual(62, slot.Left);
#endif
		}
	}
}
