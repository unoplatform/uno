using System.Threading.Tasks;
using Microsoft.UI;
using Private.Infrastructure;
using Silk.NET.OpenGL;
using Uno.UI.RuntimeTests.Helpers;
using Uno.WinUI.Graphics3DGL;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Controls;

[TestClass]
[RunsOnUIThread]
public class Given_GLCanvasElement
{
	[TestMethod]
	[GitHubWorkItem("https://github.com/unoplatform/uno/issues/24497")]
	public async Task When_Cleared_Then_Shows_Clear_Color()
	{
		var SUT = new ClearColorGLCanvasElement { Width = 100, Height = 100 };

		await UITestHelper.Load(SUT);
		await UITestHelper.WaitFor(() => SUT.IsGLInitialized is not null, timeoutMS: 5000);

		// Without a usable OpenGL context the element gives up before its first render.
		if (SUT.IsGLInitialized == false && SUT.RenderCount == 0)
		{
			Assert.Inconclusive("No usable OpenGL context on this platform.");
		}

		await UITestHelper.WaitFor(() => SUT.RenderCount > 0, timeoutMS: 5000);
		await UITestHelper.WaitForRender(2);

		// A failed framebuffer readback disables the element (IsGLInitialized becomes false) and leaves it blank.
		Assert.IsTrue(SUT.IsGLInitialized, "The element stopped rendering; see the GLCanvasElement error in the log.");

		// Pure red, so a readback that swaps red and blue the wrong way shows up as blue.
		var bitmap = await UITestHelper.ScreenShot(SUT);
		ImageAssert.HasColorAt(bitmap, bitmap.Width / 2, bitmap.Height / 2, Colors.Red, tolerance: 5);
	}

	private class ClearColorGLCanvasElement : GLCanvasElement
	{
		public ClearColorGLCanvasElement() : base(null) { }

		public int RenderCount { get; private set; }

		protected override void Init(GL gl) { }

		protected override void RenderOverride(GL gl)
		{
			RenderCount++;
			gl.ClearColor(1, 0, 0, 1);
			gl.Clear(ClearBufferMask.ColorBufferBit);
		}

		protected override void OnDestroy(GL gl) { }
	}
}
