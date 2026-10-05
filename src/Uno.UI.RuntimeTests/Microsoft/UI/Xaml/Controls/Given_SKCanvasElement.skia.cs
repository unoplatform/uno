using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Private.Infrastructure;
using SkiaSharp;
using Uno.Foundation.Extensibility;
using Uno.Graphics;
using Uno.UI.RuntimeTests.Helpers;
using Uno.WinUI.Graphics2DSK;
using Size = Windows.Foundation.Size;
namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_SKCanvasElement
{
	[TestMethod]
	public async Task When_Clipped_Inside_ScrollViewer()
	{
		var SUT = new BlueFillSKCanvasElement
		{
			Height = 400,
			Width = 400
		};

		var border = new Border
		{
			BorderBrush = Microsoft.UI.Colors.Green,
			Height = 400,
			Child = new ScrollViewer
			{
				VerticalAlignment = VerticalAlignment.Top,
				Height = 100,
				Background = Microsoft.UI.Colors.Red,
				Content = SUT
			}
		};

		await UITestHelper.Load(border);

		// A backend without a native SKCanvas (WebGPU) draws through a GL island that comes up a few frames after load;
		// its output composites on the frame after its first render.
		await UITestHelper.WaitFor(() => SUT.Rendered, timeoutMS: 5000);
		await UITestHelper.WaitForRender(2);

		var bitmap = await UITestHelper.ScreenShot(border);

		ImageAssert.HasColorInRectangle(bitmap, new Rectangle(0, 0, 400, 300), Microsoft.UI.Colors.Blue);
		ImageAssert.DoesNotHaveColorInRectangle(bitmap, new Rectangle(0, 101, 400, 299), Microsoft.UI.Colors.Blue);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/brain-products-private/issues/14")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaAndroid)] // https://github.com/unoplatform/uno/issues/22665
	public async Task When_Waiting_For_Another_Thread()
	{
		if (OperatingSystem.IsBrowser())
		{
			Assert.Inconclusive("This test on WASM throws an Uncaught ManagedError: Cannot wait on monitors on this runtime.");
		}
		var SUT = new TaskWaitingSKCanvasElement() { Width = 400, Height = 400 };
		await UITestHelper.Load(SUT);
		await Task.Delay(3000);
		Assert.IsFalse(SUT.RenderOverrideCalledNestedly);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/brain-products-private/issues/14")]
	[PlatformCondition(ConditionMode.Exclude, RuntimeTestPlatforms.SkiaAndroid)] // https://github.com/unoplatform/uno/issues/22665
	public async Task When_Waiting_For_Another_Thread2()
	{
		if (OperatingSystem.IsBrowser())
		{
			Assert.Inconclusive("This test requires a multithreaded environment.");
		}
		var gate = new object();
		var SUT = new LockWaitingSKCanvasElement(gate) { Width = 400, Height = 400 };
		await UITestHelper.Load(SUT);
		_ = Task.Run(() =>
		{
			while (SUT.IsLoaded)
			{
				lock (gate)
				{
					Thread.Sleep(200);
				}
				// On Android, we need this additional delay because otherwise, this thread will reacquire the lock
				// after releasing it before the UI thread has a chance to acquire the lock in
				// LockWaitingSKCanvasElement.RenderOverride.
				Thread.Sleep(200);
			}
		});
		await Task.Delay(3000);
		Assert.IsFalse(SUT.RenderOverrideCalledNestedly);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24699")]
	public void When_Graphics3DGL_Not_Referenced_By_Graphics2DSK()
	{
		// AOT and eager linkers (Android AOT, .NET 11 iOS CoreTypeMap) fail on a reference the package doesn't carry.
		var references = typeof(SKCanvasElement).Assembly.GetReferencedAssemblies();

		Assert.IsFalse(
			references.Any(r => r.Name is "Uno.WinUI.Graphics3DGL" or "Silk.NET.OpenGL"),
			$"Graphics2DSK must not reference Graphics3DGL or Silk; found: {string.Join(", ", references.Select(r => r.Name))}");
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24699")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public void When_GL_Island_Registered_By_Graphics3DGL()
	{
		// SKCanvasElement falls back to the GL island only through this registration.
		Assert.IsTrue(ApiExtensibility.IsRegistered<IGLIsland>());
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24699")]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaDesktop)]
	public async Task When_GL_Island_Renders_Or_Reports_Unavailable()
	{
		var renderer = new RecordingGLIslandRenderer();
		Assert.IsTrue(ApiExtensibility.CreateInstance<IGLIsland>(renderer, out var island));
		island.Element.Width = 100;
		island.Element.Height = 100;

		try
		{
			await UITestHelper.Load(island.Element);
			await UITestHelper.WaitFor(() => renderer.Rendered || renderer.Unavailable, timeoutMS: 5000);

			if (renderer.Rendered)
			{
				Assert.IsTrue(renderer.Initialized);
				Assert.AreNotEqual(0u, renderer.Framebuffer);
				Assert.AreEqual((100, 100), renderer.Size);
			}
		}
		finally
		{
			TestServices.WindowHelper.WindowContent = null;
		}
	}

	private class RecordingGLIslandRenderer : IGLIslandRenderer
	{
		public bool Initialized { get; private set; }
		public bool Rendered { get; private set; }
		public bool Unavailable { get; private set; }
		public uint Framebuffer { get; private set; }
		public (int Width, int Height) Size { get; private set; }

		public void Init() => Initialized = true;

		public void Render(uint framebuffer, int width, int height)
		{
			Rendered = true;
			Framebuffer = framebuffer;
			Size = (width, height);
		}

		public void Destroy() { }

		public void OnUnavailable() => Unavailable = true;
	}

	private class BlueFillSKCanvasElement : SKCanvasElement
	{
		public bool Rendered { get; private set; }

		protected override void RenderOverride(SKCanvas canvas, Size area)
		{
			Rendered = true;
			using var paint = new SKPaint { Color = SKColors.Blue };
			canvas.DrawRect(new SKRect(0, 0, (float)area.Width, (float)area.Height), paint);
		}
	}

	public class TaskWaitingSKCanvasElement : SKCanvasElement
	{
		private bool _insideRenderOverride;
		public bool RenderOverrideCalledNestedly { get; private set; }
		protected override void RenderOverride(SKCanvas canvas, Size area)
		{
			Invalidate(); // We need to invalidate before the Task.Wait() call
			RenderOverrideCalledNestedly |= _insideRenderOverride;
			_insideRenderOverride = true;
			var tcs = new TaskCompletionSource();
			_ = Task.Run(async () =>
			{
				await Task.Delay(20);
				tcs.SetResult();
			});
			tcs.Task.Wait();
			_insideRenderOverride = false;
		}
	}

	public class LockWaitingSKCanvasElement(object gate) : SKCanvasElement
	{
		private bool _insideRenderOverride;
		public bool RenderOverrideCalledNestedly { get; private set; }
		protected override void RenderOverride(SKCanvas canvas, Size area)
		{
			Invalidate(); // We need to invalidate before the lock statement
			RenderOverrideCalledNestedly |= _insideRenderOverride;
			_insideRenderOverride = true;
			Monitor.Enter(gate);
			Monitor.Exit(gate);
			_insideRenderOverride = false;
		}
	}
}
