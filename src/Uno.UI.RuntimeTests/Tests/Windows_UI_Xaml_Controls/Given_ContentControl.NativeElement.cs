using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Extensions;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Controls.Primitives;
using System.Linq;
using static Private.Infrastructure.TestServices;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls.ContentControlPages;
using Windows_UI_Xaml_Controls;

#if NETFX_CORE
using Uno.UI.Extensions;
#elif __IOS__
using UIKit;
#elif __MACOS__
using AppKit;
#else
using Uno.UI;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls
{
	public partial class Given_ContentControl
	{
#if __SKIA__
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
		}
#endif
	}
}
