using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
public class Given_MediaPlayerPresenter
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/8339")]
	public void When_Reparented_To_FrameworkElement()
	{
		// Reparent (7.0 breaking change): MediaPlayerPresenter derives directly from
		// FrameworkElement (matching WinUI), dropping the extra Border level and its
		// leaked Child/BorderBrush/BorderThickness/CornerRadius/Padding surface.
		// (Reflection only — instantiating the presenter would spin up the platform
		// media extension; video-hosting behaviour is validated by the MediaPlayerElement
		// suite on heads where the media extension is available.)
		Assert.AreEqual(typeof(FrameworkElement), typeof(MediaPlayerPresenter).BaseType);
		Assert.AreNotEqual(typeof(Border), typeof(MediaPlayerPresenter).BaseType);
	}
}
