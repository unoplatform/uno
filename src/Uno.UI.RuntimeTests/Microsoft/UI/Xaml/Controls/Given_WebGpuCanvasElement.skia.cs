using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Silk.NET.WebGPU;
using Uno.WinUI.Graphics3DWebGpu;
using static Private.Infrastructure.TestServices;
using Colors = Microsoft.UI.Colors;
using WebGpuColor = Silk.NET.WebGPU.Color;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_WebGpuCanvasElement
{
	[TestMethod]
	public async Task When_Rendered_Then_Target_Is_Displayed()
	{
		var SUT = new ClearingWebGpuCanvasElement { Width = 100, Height = 100 };
		var host = new Grid { Width = 200, Height = 200, Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Red), Children = { SUT } };

		await UITestHelper.Load(host);
		await UITestHelper.WaitFor(() => SUT.IsWebGpuInitialized is not null, timeoutMS: 5000);

		if (RuntimeTestsBackendHelper.CurrentBackend != RuntimeTestBackends.WebGpu)
		{
			Assert.IsFalse(SUT.IsWebGpuInitialized);
			Assert.IsTrue(SUT.UnavailableReported);
			Assert.AreEqual(0, SUT.RenderCount);
			return;
		}

		Assert.IsTrue(SUT.IsWebGpuInitialized);
		await UITestHelper.WaitFor(() => SUT.RenderCount > 0, timeoutMS: 5000);
		await UITestHelper.WaitForRender(2);

		var bitmap = await UITestHelper.ScreenShot(host);
		ImageAssert.HasColorAt(bitmap, 100, 100, Colors.Blue, tolerance: 2);
		ImageAssert.HasColorAt(bitmap, 10, 10, Colors.Red, tolerance: 2);

		SUT.ClearColor = new WebGpuColor(0, 1, 0, 1);
		SUT.Invalidate();
		await UITestHelper.WaitFor(() => SUT.RenderCount > 1, timeoutMS: 5000);
		await UITestHelper.WaitForRender(2);

		bitmap = await UITestHelper.ScreenShot(host);
		ImageAssert.HasColorAt(bitmap, 100, 100, Colors.Lime, tolerance: 2);
	}

	[TestMethod]
	public async Task When_Unloaded_Then_Resources_Are_Destroyed()
	{
		var SUT = new ClearingWebGpuCanvasElement { Width = 100, Height = 100 };

		await UITestHelper.Load(SUT);
		await UITestHelper.WaitFor(() => SUT.IsWebGpuInitialized is not null, timeoutMS: 5000);

		if (RuntimeTestsBackendHelper.CurrentBackend != RuntimeTestBackends.WebGpu)
		{
			Assert.Inconclusive("Requires the WebGPU backend.");
		}

		Assert.AreEqual(1, SUT.InitCount);
		Assert.AreEqual(0, SUT.DestroyCount);

		await UITestHelper.Load(new Border(), x => x.IsLoaded);

		Assert.AreEqual(1, SUT.DestroyCount);
		Assert.IsNull(SUT.IsWebGpuInitialized);
	}

	private const string DeviceLossChildVariable = "UNO_TEST_WEBGPU_DEVICE_LOSS_CHILD";

	// The device is process-wide and wgpu answers a bad call with an abort, so the loss runs in a child process: this
	// one keeps its device, and a crash shows up as a child that died without reporting.
	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWin32 | RuntimeTestPlatforms.SkiaX11 | RuntimeTestPlatforms.SkiaMacOS)]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/26086")]
	public async Task When_Device_Is_Lost_Then_Process_Survives()
	{
		if (RuntimeTestsBackendHelper.CurrentBackend != RuntimeTestBackends.WebGpu)
		{
			Assert.Inconclusive("Requires the WebGPU backend.");
		}

		if (Environment.GetEnvironmentVariable(DeviceLossChildVariable) == "1")
		{
			await RunDeviceLossScenario();
			return;
		}

		var resultsPath = Path.Combine(Path.GetTempPath(), $"webgpu-device-loss-{Guid.NewGuid():N}.xml");
		ProcessStartInfo start = new(Environment.ProcessPath!) { UseShellExecute = false };
		if (Path.GetFileNameWithoutExtension(Environment.ProcessPath) == "dotnet")
		{
			start.ArgumentList.Add(Environment.GetCommandLineArgs()[0]);
		}

		start.ArgumentList.Add($"--runtime-tests={resultsPath}");
		start.Environment[DeviceLossChildVariable] = "1";
		start.Environment["UITEST_RUNTIME_TESTS_FILTER"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(
			$"{typeof(Given_WebGpuCanvasElement).FullName}.{nameof(When_Device_Is_Lost_Then_Process_Survives)}"));

		using var child = Process.Start(start)!;
		using CancellationTokenSource timeout = new(TimeSpan.FromMinutes(3));
		try
		{
			await child.WaitForExitAsync(timeout.Token);
		}
		catch (OperationCanceledException)
		{
			child.Kill();
			Assert.Fail("The child process did not finish within 3 minutes.");
		}

		Assert.IsTrue(File.Exists(resultsPath), $"The child process died without reporting (exit code {child.ExitCode}).");
		var results = await File.ReadAllTextAsync(resultsPath);
		File.Delete(resultsPath);
		StringAssert.Contains(results, "passed=\"1\"", results);
		Assert.AreEqual(0, child.ExitCode);
	}

	private static async Task RunDeviceLossScenario()
	{
		var SUT = new DeviceDestroyingWebGpuCanvasElement { Width = 100, Height = 100 };
		var sibling = new Border { Width = 50, Height = 50, Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Red) };
		var host = new StackPanel { Width = 200, Height = 200, Children = { SUT, sibling } };

		await UITestHelper.Load(host);
		await UITestHelper.WaitFor(() => SUT.DeviceDestroyed, timeoutMS: 5000);

		// A frame: submit, acquire and present.
		sibling.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Green);
		await UITestHelper.WaitForRender(5);

		// An offscreen render and a readback, which map a buffer.
		await UITestHelper.ScreenShot(host);

		// A new window size, which configures the surface.
		var window = WindowHelper.CurrentTestWindow!.AppWindow;
		var size = window.Size;
		window.Resize(new(size.Width + 40, size.Height + 40));
		await UITestHelper.WaitForRender(5);
		window.Resize(size);
		await UITestHelper.WaitForRender(5);
	}

	private sealed unsafe class DeviceDestroyingWebGpuCanvasElement : WebGpuCanvasElement
	{
		public bool DeviceDestroyed { get; private set; }

		protected override void Init(WebGpuCanvasContext context) { }

		protected override void OnDestroy(WebGpuCanvasContext context) { }

		protected override void RenderOverride(WebGpuCanvasContext context)
		{
			if (!DeviceDestroyed)
			{
				context.Api.DeviceDestroy(context.Device);
				DeviceDestroyed = true;
			}
		}
	}

	private sealed unsafe class ClearingWebGpuCanvasElement : WebGpuCanvasElement
	{
		public WebGpuColor ClearColor { get; set; } = new(0, 0, 1, 1);

		public int InitCount { get; private set; }

		public int DestroyCount { get; private set; }

		public int RenderCount { get; private set; }

		public bool UnavailableReported { get; private set; }

		protected override void Init(WebGpuCanvasContext context) => InitCount++;

		protected override void OnDestroy(WebGpuCanvasContext context) => DestroyCount++;

		protected override void OnWebGpuUnavailable() => UnavailableReported = true;

		protected override void RenderOverride(WebGpuCanvasContext context)
		{
			var wgpu = context.Api;
			var encoder = wgpu.DeviceCreateCommandEncoder(context.Device, null);
			var attachment = new RenderPassColorAttachment
			{
				View = context.TargetView,
				DepthSlice = uint.MaxValue,
				LoadOp = LoadOp.Clear,
				StoreOp = StoreOp.Store,
				ClearValue = ClearColor,
			};
			var descriptor = new RenderPassDescriptor { ColorAttachmentCount = 1, ColorAttachments = &attachment };
			var pass = wgpu.CommandEncoderBeginRenderPass(encoder, &descriptor);
			wgpu.RenderPassEncoderEnd(pass);
			wgpu.RenderPassEncoderRelease(pass);

			var commands = wgpu.CommandEncoderFinish(encoder, null);
			wgpu.QueueSubmit(context.Queue, 1, &commands);
			wgpu.CommandBufferRelease(commands);
			wgpu.CommandEncoderRelease(encoder);

			RenderCount++;
		}
	}
}
