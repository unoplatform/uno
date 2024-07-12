#if HAS_UNO_WINUI
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Private.Infrastructure;
using Uno.UI.Xaml;
using Windows.Graphics;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Windowing;

[TestClass]
[RunsOnUIThread]
public class Given_AppWindow
{
#if __SKIA__
	[TestMethod]
	public async Task When_Resize()
	{
		if (!OperatingSystem.IsLinux() &&
			!OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("This test only supported on Windows and Linux currently.");
		}

		var appWindow = TestServices.WindowHelper.CurrentTestWindow.AppWindow;
		AppWindowChangedEventArgs args = null;
		void OnChanged(AppWindow s, AppWindowChangedEventArgs e)
		{
			args = e;
		}
		appWindow.Changed += OnChanged;
		var originalSize = appWindow.Size;
		try
		{
			var adjustedSize = new SizeInt32() { Width = originalSize.Width + 10, Height = originalSize.Height + 10 };
			appWindow.Resize(adjustedSize);
			await TestServices.WindowHelper.WaitFor(() => args is not null);
			Assert.IsTrue(args.DidSizeChange);
			Assert.AreEqual(appWindow.Size, adjustedSize);
		}
		finally
		{
			appWindow.Resize(originalSize);
			await TestServices.WindowHelper.WaitFor(() => appWindow.Size.Equals(originalSize));
		}
	}

	[TestMethod]
	public async Task When_Move()
	{
		if (!OperatingSystem.IsLinux() &&
			!OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("This test only supported on Windows and Linux currently.");
		}

		var appWindow = TestServices.WindowHelper.CurrentTestWindow.AppWindow;
		AppWindowChangedEventArgs args = null;
		void OnChanged(AppWindow s, AppWindowChangedEventArgs e)
		{
			args = e;
		}
		appWindow.Changed += OnChanged;
		var originalPosition = appWindow.Position;
		try
		{
			var adjustedPosition = new PointInt32() { X = originalPosition.X + 10, Y = originalPosition.Y + 10 };
			appWindow.Move(adjustedPosition);
			await TestServices.WindowHelper.WaitFor(() => args is not null);
			Assert.IsTrue(args.DidPositionChange);
			Assert.AreEqual(appWindow.Position, adjustedPosition);
		}
		finally
		{
			appWindow.Move(originalPosition);
			await TestServices.WindowHelper.WaitFor(() => appWindow.Position.Equals(originalPosition));
		}
	}
#endif
}
#endif
