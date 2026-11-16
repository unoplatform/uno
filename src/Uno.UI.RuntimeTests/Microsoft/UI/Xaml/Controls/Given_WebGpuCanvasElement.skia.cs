using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Uno.UI.RuntimeTests.Helpers;
using Silk.NET.WebGPU;
using Uno.WinUI.Graphics3DWebGpu;
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
