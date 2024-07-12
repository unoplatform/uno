#if HAS_UNO_WINUI
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Private.Infrastructure;
using Windows.Graphics;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Windowing;

[TestClass]
[RunsOnUIThread]
public class Given_AppWindow
{
#if __SKIA__
	[TestMethod]
	public void When_Resize()
	{
		if (!OperatingSystem.IsLinux() &&
			!OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("This test only supported on Windows and Linux currently.");
		}

		var appWindow = TestServices.WindowHelper.CurrentTestWindow.AppWindow;
		var originalSize = appWindow.Size;
		try
		{
			var adjustedSize = new SizeInt32() { Width = originalSize.Width + 10, Height = originalSize.Height + 10 };
			appWindow.Resize(adjustedSize);
			Assert.AreEqual(appWindow.Size, adjustedSize);
		}
		finally
		{
			appWindow.Resize(originalSize);
		}
	}

	[TestMethod]
	public void When_Move()
	{
		if (!OperatingSystem.IsLinux() &&
			!OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("This test only supported on Windows and Linux currently.");
		}

		var appWindow = TestServices.WindowHelper.CurrentTestWindow.AppWindow;
		var originalPosition = appWindow.Position;
		try
		{
			var adjustedPosition = new PointInt32() { X = originalPosition.X + 10, Y = originalPosition.Y + 10 };
			appWindow.Move(adjustedPosition);
			Assert.AreEqual(appWindow.Position, adjustedPosition);
		}
		finally
		{
			appWindow.Move(originalPosition);
		}
	}
#endif
}
#endif
