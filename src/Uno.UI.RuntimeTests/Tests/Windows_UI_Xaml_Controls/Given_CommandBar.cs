using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Input.Preview.Injection;
using Uno.UI.RuntimeTests.Helpers;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using static Private.Infrastructure.TestServices;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	[TestClass]
	public class Given_CommandBar
	{
		[TestMethod]
		[RunsOnUIThread]
#if !__SKIA__
		[Ignore("InputInjector is only supported on skia")]
#endif
		public async Task When_Popup_Open_Then_Click_Outside()
		{
			var SUT = new CommandBar
			{
				SecondaryCommands =
				{
					new AppBarButton
					{
						Label = "secondary",
						Name = "SecondaryButton"
					}
				}
			};

			WindowHelper.WindowContent = SUT;
			await WindowHelper.WaitForIdle();

			var moreButton = (Button)SUT.FindName("MoreButton");

			var injector = InputInjector.TryCreate() ?? throw new InvalidOperationException("Failed to init the InputInjector");
			using var finger = injector.GetFinger();

			Point GetCenter(Rect rect) => new Point(rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
			finger.Press(GetCenter(moreButton.GetAbsoluteBounds()));
			finger.Release();

			await WindowHelper.WaitForIdle();

			var popups = VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot);
			Assert.AreEqual(1, popups.Count);

			var secondaryButton = (AppBarButton)SUT.FindName("SecondaryButton");
			var bounds = secondaryButton.GetAbsoluteBounds();
			finger.Press(bounds.Bottom + 10, (bounds.Left + bounds.Right) / 2);
			finger.Release();

			await WindowHelper.WaitForIdle();

			Assert.AreEqual(0, VisualTreeHelper.GetOpenPopupsForXamlRoot(SUT.XamlRoot).Count);
		}
	}
}
