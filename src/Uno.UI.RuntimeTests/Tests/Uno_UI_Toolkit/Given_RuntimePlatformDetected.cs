using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Private.Infrastructure;
using Uno.Disposables;
using Uno.UI.RuntimeTests.Extensions;
using Uno.UI.RuntimeTests.Helpers;
using Uno.UI.Toolkit;
using Uno.UI.Helpers;
using Windows.UI;
using Windows.UI.ViewManagement;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using static Private.Infrastructure.TestServices;
using FluentAssertions;

namespace Uno.UI.RuntimeTests.Tests.Uno_UI_Toolkit
{
	[TestClass]
	[RunsOnUIThread]
	public class Given_RuntimePlatformDetected
	{
#if __SKIA__
		[TestMethod]
		public void When_IsSkia()
		{
			Uno.UI.Helpers.PlatformRuntimeHelper.Current.IsSkia().Should().BeTrue();
		}
#endif

#if __IOS__
		[TestMethod]
		public void When_IsIOS()
		{
			Uno.UI.Helpers.PlatformRuntimeHelper.Current.IsIOS().Should().BeTrue();
		}
#endif

#if __ANDROID__
		[TestMethod]
		public void When_IsAndroid()
		{
			Uno.UI.Helpers.PlatformRuntimeHelper.Current.IsAndroid().Should().BeTrue();
		}
#endif

#if __MACCATALYST__
		[TestMethod]
		public void When_IsMacCatalyst()
		{
			Uno.UI.Helpers.PlatformRuntimeHelper.Current.IsMacCatalyst().Should().BeTrue();
		}
#endif

#if !HAS_UNO
		[TestMethod]
		public void When_IsWindows()
		{
			Uno.UI.Toolkit.PlatformRuntimeHelper.Current.Should().Be(Uno.UI.Toolkit.UnoRuntimePlatform.NativeWinUI);
		}
#else
		[TestMethod]
		public void When_IsUnoIsKnown()
		{
			Uno.UI.Helpers.PlatformRuntimeHelper.Current.Should().NotBe(Uno.UI.Toolkit.UnoRuntimePlatform.Unknown);
		}
#endif
	}
}
