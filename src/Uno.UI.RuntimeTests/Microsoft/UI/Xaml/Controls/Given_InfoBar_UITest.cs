#nullable enable

using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_InfoBar_UITest
{
	// Migrated from SamplesApp.UITests InfoBarTests/Given_InfoBar.cs (IsClosableTest, AccessibilityViewTest).
	// The original was [Ignore]d as a Selenium/native stabilization artifact; as a deterministic Skia
	// runtime test driving the control's properties directly it is reliable, so it runs enabled.

	[TestMethod]
	public async Task When_IsClosable_Toggles_CloseButton_Visibility()
	{
		var infoBar = new InfoBar
		{
			Title = "Title",
			Message = "Message",
			IsOpen = true,
		};

		try
		{
			await UITestHelper.Load(infoBar);

			var closeButton = FindChild<Button>(infoBar, "CloseButton");
			Assert.IsNotNull(closeButton, "Close button should exist in the InfoBar template.");
			Assert.AreEqual(Visibility.Visible, closeButton.Visibility, "Close button should be visible by default (IsClosable=true).");

			infoBar.IsClosable = false;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Collapsed, closeButton.Visibility, "Close button should be collapsed when IsClosable=false.");

			infoBar.IsClosable = true;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Visible, closeButton.Visibility, "Close button should be visible again when IsClosable=true.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_IsOpen_Toggled_Then_Content_Visibility_Updates()
	{
		var infoBar = new InfoBar
		{
			Title = "Title",
			Message = "Message",
			IsOpen = true,
		};

		try
		{
			await UITestHelper.Load(infoBar);

			var contentRoot = FindChild<FrameworkElement>(infoBar, "ContentRoot");
			Assert.IsNotNull(contentRoot, "ContentRoot should exist in the InfoBar template.");
			Assert.AreEqual(Visibility.Visible, contentRoot.Visibility, "InfoBar content should be visible when IsOpen=true.");

			infoBar.IsOpen = false;
			await WindowHelper.WaitForIdle();
			Assert.AreEqual(Visibility.Collapsed, contentRoot.Visibility, "InfoBar content should be collapsed when IsOpen=false.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	[TestMethod]
	public async Task When_Default_InfoBar_Then_Content_Collapsed()
	{
		// A newly created InfoBar defaults to IsOpen=false, so its content is collapsed and the
		// control has zero rendered size. Wait on the template part appearing instead of the
		// default non-zero-size loaded check, which would otherwise time out.
		var infoBar = new InfoBar
		{
			Title = "Title",
			Message = "Message",
		};

		try
		{
			await UITestHelper.Load(infoBar, i => FindChild<FrameworkElement>(i, "ContentRoot") is not null);

			var contentRoot = FindChild<FrameworkElement>(infoBar, "ContentRoot");
			Assert.IsNotNull(contentRoot, "ContentRoot should exist in the InfoBar template.");
			Assert.AreEqual(Visibility.Collapsed, contentRoot.Visibility, "A default (IsOpen=false) InfoBar should keep its content collapsed.");
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static T? FindChild<T>(DependencyObject root, string name) where T : FrameworkElement
	{
		var count = VisualTreeHelper.GetChildrenCount(root);
		for (var i = 0; i < count; i++)
		{
			var child = VisualTreeHelper.GetChild(root, i);
			if (child is T fe && fe.Name == name)
			{
				return fe;
			}

			if (FindChild<T>(child, name) is { } found)
			{
				return found;
			}
		}

		return null;
	}
}
