using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives.PopupPages;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls_Primitives
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_Popup
	{
		[TestMethod]
#if __MACOS__
		[Ignore("Currently fails on macOS, part of #9282 epic")]
#endif
		public async Task Check_Can_Reach_Main_Visual_Tree()
		{
			var page = new ReachMainTreePage();
			WindowHelper.WindowContent = page;

			await WindowHelper.WaitForLoaded(page);

			Assert.IsTrue(CanReach(page.DummyTextBlock, page));

			try
			{
				page.TargetPopup.IsOpen = true;
				await WindowHelper.WaitForLoaded(page.PopupButton);

				Assert.IsTrue(CanReach(page.PopupButton, page));
			}
			finally
			{
				page.TargetPopup.IsOpen = false;
			}
		}

#if __ANDROID__
		[TestMethod]
		public async Task Check_Can_Reach_Main_Visual_Tree_Alternate_Mode()
		{
			var originalConfig = FeatureConfiguration.Popup.UseNativePopup;
			FeatureConfiguration.Popup.UseNativePopup = !originalConfig;
			try
			{
				await Check_Can_Reach_Main_Visual_Tree();
			}
			finally
			{
				FeatureConfiguration.Popup.UseNativePopup = originalConfig;
			}
		}
#endif

		[TestMethod]
		public void When_IsLightDismissEnabled_Default()
		{
			var popup = new Popup();
			Assert.IsFalse(popup.IsLightDismissEnabled);
		}

		[TestMethod]
		public void When_Closed_Immediately()
		{
			var popup = new Popup();
			popup.IsOpen = true;
			// Should not throw
			popup.IsOpen = false;
		}

		private static bool CanReach(DependencyObject startingElement, DependencyObject targetElement)
		{
			var currentElement = startingElement;
			while (currentElement != null)
			{
				if (currentElement == targetElement)
				{
					return true;
				}

				// Quoting WCT DataGrid:
				//		// Walk up the visual tree. Try using the framework element's
				//		// parent.  We do this because Popups behave differently with respect to the visual tree,
				//		// and it could have a parent even if the VisualTreeHelper doesn't find it.
				DependencyObject parent = null;
				if (currentElement is FrameworkElement fe)
				{
					parent = fe.Parent;
				}
				if (parent == null)
				{
					parent = VisualTreeHelper.GetParent(currentElement);
				}

				currentElement = parent;
			}

			// Did not hit targetElement
			return false;
		}
	}
}
