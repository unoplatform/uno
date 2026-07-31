using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.UI;

#if __SKIA__
using Uno.UI.RuntimeTests.Helpers;
#endif

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_Visual_ArrangePending
{
	// A child its parent measured but never arranged has no layout slot, and a visual's content is not
	// bounded by its Size, so painting it would draw at the parent's origin, unclipped — several such
	// children stack into one another. WinUI never renders an element its parent didn't arrange.
	[TestMethod]
	[RunsOnUIThread]
#if !__SKIA__
	[Ignore("Only Skia renders the visual tree through Uno's compositor.")]
#endif
	public async Task When_Child_Never_Arranged_Then_It_Does_Not_Paint()
	{
#if __SKIA__
		var sentinel = new Border
		{
			Width = 60,
			Height = 60,
			Background = new SolidColorBrush(Colors.Magenta),
		};

		var host = new NeverArrangesChildrenPanel
		{
			Background = new SolidColorBrush(Colors.White),
		};
		host.Children.Add(sentinel);

		await UITestHelper.Load(host);

		var screenshot = await UITestHelper.ScreenShot(host);

		// The sentinel would paint at the panel's origin if unarranged children were rendered.
		ImageAssert.DoesNotHaveColorAt(screenshot, 5, 5, Colors.Magenta, tolerance: 10);
		ImageAssert.DoesNotHaveColorAt(screenshot, 30, 30, Colors.Magenta, tolerance: 10);
#else
		await Task.CompletedTask;
#endif
	}

	private partial class NeverArrangesChildrenPanel : Panel
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			foreach (var child in Children)
			{
				child.Measure(availableSize);
			}

			return new Size(120, 120);
		}

		// Deliberately arranges nothing.
		protected override Size ArrangeOverride(Size finalSize) => finalSize;
	}
}
