﻿#if HAS_UNO_WINUI || WINDOWS
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Private.Infrastructure;
using Windows.Graphics;

namespace Uno.UI.RuntimeTests.Tests.Microsoft_UI_Windowing;

[TestClass]
[RunsOnUIThread]
#if !__SKIA__ && !WINDOWS
[Ignore]
#endif
public class Given_AppWindow
{
	[TestMethod]
	public async Task When_Resize()
	{
		if (OperatingSystem.IsLinux())
		{
			// Currently failing in CI #9080
			Assert.Inconclusive();
		}

		AssertPositioningAndSizingSupport();

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
		AssertPositioningAndSizingSupport();

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

	[TestMethod]
	public async Task When_Resize_Before_Activate()
	{
		AssertPositioningAndSizingSupport();

		var newWindow = new Window();
		var appWindow = newWindow.AppWindow;
		AppWindowChangedEventArgs args = null;
		void OnChanged(AppWindow s, AppWindowChangedEventArgs e)
		{
			args = e;
		}
		appWindow.Changed += OnChanged;
		var originalSize = appWindow.Size;
		var activated = false;
		try
		{
			var adjustedSize = new SizeInt32() { Width = originalSize.Width + 10, Height = originalSize.Height + 10 };
			appWindow.Resize(adjustedSize);
			await TestServices.WindowHelper.WaitFor(() => args is not null);
			Assert.IsTrue(args.DidSizeChange);
			Assert.AreEqual(appWindow.Size, adjustedSize);

			newWindow.Activated += (s, e) => activated = true;
			newWindow.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);

			Assert.AreEqual(appWindow.Size, adjustedSize);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.RunOnUIThread(() => newWindow.Close());
		}
	}

	[TestMethod]
	public async Task When_Move_Before_Activate()
	{
		AssertPositioningAndSizingSupport();

		var newWindow = new Window();
		var appWindow = newWindow.AppWindow;
		AppWindowChangedEventArgs args = null;
		void OnChanged(AppWindow s, AppWindowChangedEventArgs e)
		{
			args = e;
		}
		appWindow.Changed += OnChanged;
		var originalPosition = appWindow.Position;
		var activated = false;
		try
		{
			var adjustedPosition = new PointInt32() { X = 40, Y = 40 };
			appWindow.Move(adjustedPosition);
			await TestServices.WindowHelper.WaitFor(() => args is not null);
			Assert.IsTrue(args.DidPositionChange);
			Assert.AreEqual(appWindow.Position, adjustedPosition);

			newWindow.Activated += (s, e) => activated = true;
			newWindow.Activate();
			await TestServices.WindowHelper.WaitFor(() => activated);

			Assert.AreEqual(appWindow.Position, adjustedPosition);
		}
		finally
		{
			await TestServices.WindowHelper.WaitForIdle();
			await TestServices.RunOnUIThread(() => newWindow.Close());
		}
	}

	private void AssertPositioningAndSizingSupport()
	{
		if (!OperatingSystem.IsLinux() &&
			!OperatingSystem.IsWindows() &&
			!OperatingSystem.IsMacOS() ||
			TestServices.WindowHelper.IsXamlIsland ||
			IsGtk())
		{
			Assert.Inconclusive("This test only supported on Windows, macOS and Linux apps currently.");
		}
	}

	private bool IsGtk()
	{
		var assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly a in assemblies)
		{
			if (a.GetName().Name == "GtkSharp")
			{
				return true;
			}
		}

		return false;
	}
}
#endif
