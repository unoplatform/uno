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
		[TestMethod]
		public void When_Native_Element()
		{
			var checkButtonType =
				Type.GetType("Gtk.CheckButton, GtkSharp", false)
				?? Type.GetType("System.Windows.Controls.CheckBox, PresentationFramework", false);

			Assert.IsNotNull(checkButtonType);

			var nativeControl = Activator.CreateInstance(checkButtonType);

			ContentPresenter SUT = new();
			SUT.Content = nativeControl;

			TestServices.WindowHelper.WindowContent = SUT;

			Assert.IsTrue(ContentPresenter.IsNativeElementAttached(SUT.XamlRoot, nativeControl));
		}

		[TestMethod]
		public async Task When_Native_Element_Detached()
		{
			var checkButtonType =
				Type.GetType("Gtk.CheckButton, GtkSharp", false)
				?? Type.GetType("System.Windows.Controls.CheckBox, PresentationFramework", false);

			Assert.IsNotNull(checkButtonType);

			var nativeControl = Activator.CreateInstance(checkButtonType);

			ContentPresenter SUT = new();
			SUT.Content = nativeControl;

			TestServices.WindowHelper.WindowContent = SUT;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsTrue(ContentPresenter.IsNativeElementAttached(SUT.XamlRoot, nativeControl));

			TestServices.WindowHelper.WindowContent = null;
			await TestServices.WindowHelper.WaitForIdle();

			Assert.IsFalse(ContentPresenter.IsNativeElementAttached(SUT.XamlRoot, nativeControl));
		}
	}
}
#endif
