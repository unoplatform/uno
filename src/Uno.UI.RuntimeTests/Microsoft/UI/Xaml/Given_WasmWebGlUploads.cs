using Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation;

namespace Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml;

[TestClass]
[RunsOnUIThread]
public class Given_WasmWebGlUploads
{
	// Skia uploads glyph atlas patches with UNPACK_ROW_LENGTH set, from a view covering only width * height bytes.
	private const string StridedUploadScript = """
		(function () {
			const gl = document.createElement('canvas').getContext('webgl2');
			if (!gl) { return 'no-webgl2'; }
			globalThis.Uno.UI.Runtime.EmscriptenWebGL.fixStridedUploads(gl);

			const texture = gl.createTexture();
			gl.bindTexture(gl.TEXTURE_2D, texture);
			gl.texStorage2D(gl.TEXTURE_2D, 1, gl.R8, 64, 64);
			gl.pixelStorei(gl.UNPACK_ALIGNMENT, 1);
			gl.pixelStorei(gl.UNPACK_ROW_LENGTH, 64);

			const heap = new Uint8Array(64 * 64);
			gl.texSubImage2D(gl.TEXTURE_2D, 0, 4, 4, 8, 8, gl.RED, gl.UNSIGNED_BYTE, new Uint8Array(heap.buffer, 0, 8 * 8));
			const error = gl.getError();

			gl.deleteTexture(texture);
			gl.getExtension('WEBGL_lose_context')?.loseContext();
			return String(error);
		})()
		""";

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public void When_Upload_Uses_Row_Length_Then_Short_View_Is_Accepted()
	{
		var result = WasmSemanticDomHelper.InvokeBrowserJs(StridedUploadScript);
		if (result == "no-webgl2")
		{
			Assert.Inconclusive("WebGL2 is not available.");
		}

		Assert.AreEqual("0", result, "texSubImage2D should not raise a GL error (1282 = INVALID_OPERATION).");
	}

	[TestMethod]
	[PlatformCondition(ConditionMode.Include, RuntimeTestPlatforms.SkiaWasm)]
	public void When_Rendering_With_WebGl_Then_Strided_Uploads_Are_Fixed()
	{
		var result = WasmSemanticDomHelper.InvokeBrowserJs("""
			(function () {
				const ctx = globalThis.GL?.currentContext?.GLctx;
				return ctx ? String(!!ctx.__unoStridedUploadsFixed) : 'no-webgl';
			})()
			""");
		if (result == "no-webgl")
		{
			Assert.Inconclusive("The app is not rendering through WebGL.");
		}

		Assert.AreEqual("true", result);
	}
}
