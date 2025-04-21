#if __SKIA__
using System;
using System.Threading.Tasks;
using Private.Infrastructure;
using Uno.UI.Xaml;
using Windows.UI.Core;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_ContentControl
	{
		[ConditionalTest(IgnoredPlatforms = ~(RuntimeTestPlatforms.SkiaWpf))]
		public async Task When_Native_Element()
		{
			var checkButtonType =
				RuntimeTestsPlatformHelper.CurrentPlatform is RuntimeTestPlatforms.SkiaWpf
					? Type.GetType("System.Windows.Controls.CheckBox, PresentationFramework", false)
					: Type.GetType("Gtk.CheckButton, GtkSharp", false);

			if (checkButtonType is null)
			{
				Assert.Inconclusive("No native button element found on this platform.");
			}

			var nativeControl = Activator.CreateInstance(checkButtonType);

			ContentPresenter SUT = new();
			SUT.Content = nativeControl;

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.IsNativeHost);
		}

		[ConditionalTest(IgnoredPlatforms = (RuntimeTestPlatforms.Skia | RuntimeTestPlatforms.Native) & ~(RuntimeTestPlatforms.SkiaWpf))]
		public async Task When_Native_Element_Detached()
		{
			var checkButtonType =
				RuntimeTestsPlatformHelper.CurrentPlatform is RuntimeTestPlatforms.SkiaWpf
					? Type.GetType("System.Windows.Controls.CheckBox, PresentationFramework", false)
					: Type.GetType("Gtk.CheckButton, GtkSharp", false);

			if (checkButtonType is null)
			{
				Assert.Inconclusive("No native button element found on this platform.");
			}

			var nativeControl = Activator.CreateInstance(checkButtonType);

			ContentPresenter SUT = new();
			SUT.Content = nativeControl;

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(SUT.IsNativeHost);

			SUT.Content = "something that isn't a native element";
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(SUT.IsNativeHost);
		}
	}
}
#endif
