using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
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

		var bitmap = await UITestHelper.ScreenShot(border);

		ImageAssert.HasColorInRectangle(bitmap, new Rectangle(0, 0, 400, 300), Microsoft.UI.Colors.Blue);
		ImageAssert.DoesNotHaveColorInRectangle(bitmap, new Rectangle(0, 101, 400, 299), Microsoft.UI.Colors.Blue);
	}

	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/brain-products-private/issues/14")]
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
			}
		});
		await Task.Delay(3000);
		Assert.IsFalse(SUT.RenderOverrideCalledNestedly);
	}

	[TestMethod]
	public async Task When_Loading_Not_Rendereing_Unnecessarily()
	{
		var renderInvalidatedCount = 0;

		var SUT = new RedFillSKCanvasElement
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
				Height = 400,
				Background = Microsoft.UI.Colors.Red,
				Content = SUT
			}
		};

		await UITestHelper.Load(border);

		SUT.XamlRoot.RenderInvalidated += () =>
		{
			renderInvalidatedCount++;
		};

		// Wait a short time to ensure no extra invalidations occur
		await Task.Delay(6000);

		Assert.IsTrue(renderInvalidatedCount < 4, "RenderInvalidated should not be executed indefinitely.");
	}


	[TestMethod]
	public async Task When_Invalidate_Called_MultipleTimes_DoesNot_Crash()
	{
		var SUT = new RedFillSKCanvasElement
		{
			Height = 200,
			Width = 400
		};

		var blueCanvas = new BlueFillSKCanvasElement
		{
			Height = 200,
			Width = 400
		};

		var stack = new StackPanel
		{
			VerticalAlignment = VerticalAlignment.Stretch,
			HorizontalAlignment = HorizontalAlignment.Stretch,
			Height = 500,
		};

		stack.Children.Add(SUT);
		stack.Children.Add(blueCanvas);

		var border = new Border
		{
			BorderBrush = Microsoft.UI.Colors.Green,
			Height = 500,
			Child = stack
		};

		await UITestHelper.Load(border);

		var bitmap = await UITestHelper.ScreenShot(border);

		ImageAssert.HasColorInRectangle(bitmap, new Rectangle(0, 0, 200, 200), Microsoft.UI.Colors.Red);
		ImageAssert.HasColorInRectangle(bitmap, new Rectangle(0, 200, 200, 200), Microsoft.UI.Colors.Blue);
	}

	private class BlueFillSKCanvasElement : SKCanvasElement
	{
		protected override void RenderOverride(SKCanvas canvas, Size area)
		{
			canvas.DrawRect(new SKRect(0, 0, (float)area.Width, (float)area.Height), new SKPaint { Color = SKColors.Blue });
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

	private class RedFillSKCanvasElement : SKCanvasElement
	{
		protected override void RenderOverride(SKCanvas canvas, Size area)
		{
			canvas.DrawRect(new SKRect(0, 0, (float)area.Width, (float)area.Height), new SKPaint { Color = SKColors.Red });
			InvalidateMeasure();
		}
	}
}
