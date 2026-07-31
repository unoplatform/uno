using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Uno.UI.RuntimeTests.Helpers;
using static Private.Infrastructure.TestServices;
using Windows.Foundation;
using Windows.UI;
using Rectangle = System.Drawing.Rectangle;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Composition;

[TestClass]
public class Given_Visual_ArrangePending
{
	// A child its parent measured but never arranged has no layout slot. Text is laid out from measure,
	// so its ink exists independently of any arrange, and a visual's content is not bounded by its Size —
	// several such children would stack at the parent's origin. WinUI paints nothing for an element its
	// parent didn't arrange, so this runs on the WinUI head too and pins that parity.
	// A TextBlock (not a Border) is required: a Border paints only inside its Size, which is 0x0 while
	// unarranged, so it would pass whether or not the suppression works.
	[TestMethod]
	[RunsOnUIThread]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.NativeAndroid | RuntimeTestPlatforms.NativeIOS | RuntimeTestPlatforms.NativeWasm)]
	public async Task When_Child_Never_Arranged_Then_It_Does_Not_Paint()
	{
		var suppressed = new NeverArrangesChildrenPanel
		{
			Background = new SolidColorBrush(Colors.White),
			Children = { MakeInk() },
		};

		// Same content in a panel that arranges normally: proves the assertion below can fail, and that
		// the suppression is not simply hiding everything.
		var control = new StackPanel
		{
			Background = new SolidColorBrush(Colors.White),
			Children = { MakeInk() },
		};

		try
		{
			await UITestHelper.Load(new StackPanel { Children = { suppressed, control } });

			var controlShot = await UITestHelper.ScreenShot(control);
			ImageAssert.HasColorInRectangle(
				controlShot,
				new Rectangle(0, 0, (int)controlShot.Width, (int)controlShot.Height),
				Colors.Black,
				tolerance: 100);

			var suppressedShot = await UITestHelper.ScreenShot(suppressed);
			ImageAssert.DoesNotHaveColorInRectangle(
				suppressedShot,
				new Rectangle(0, 0, (int)suppressedShot.Width, (int)suppressedShot.Height),
				Colors.Black,
				tolerance: 100);
		}
		finally
		{
			WindowHelper.WindowContent = null;
		}
	}

	private static TextBlock MakeInk() => new()
	{
		Text = "██████",
		FontSize = 24,
		Foreground = new SolidColorBrush(Colors.Black),
	};

	private partial class NeverArrangesChildrenPanel : Panel
	{
		protected override Size MeasureOverride(Size availableSize)
		{
			foreach (var child in Children)
			{
				child.Measure(availableSize);
			}

			return new Size(200, 60);
		}

		// Deliberately arranges nothing.
		protected override Size ArrangeOverride(Size finalSize) => finalSize;
	}
}
