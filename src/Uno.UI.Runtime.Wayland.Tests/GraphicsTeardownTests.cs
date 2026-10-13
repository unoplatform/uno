#nullable enable

using System.Numerics;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;
using Uno.UI.Composition.Drawing;
using Uno.UI.Runtime.Wayland.Rendering;
using Windows.Foundation;
using Windows.UI;

namespace Uno.UI.Runtime.Wayland.Tests;

[TestClass]
public sealed class GraphicsTeardownTests
{
	[TestMethod]
	[DataRow(false)]
	[DataRow(true)]
	public void SkiaAbandonmentAfterContextLossDoesNotCallGl(bool releaseResourcesControl)
	{
		using var gl = new MockGles();
		using var factory = new SkiaDrawingFactory(gl);
		using var target = new GlTarget();
		using (factory.BeginPresent(target, ReadOnlySpan<Rect>.Empty))
		{
		}

		// Inspect the context created by the production loader, without adding a backend injection seam.
		var context = (GRContext)typeof(SkiaDrawingFactory)
			.GetField("_glContext", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(factory)!;
		Assert.IsFalse(context.IsAbandoned);
		using var surface = SKSurface.Create(context, true, new SKImageInfo(2, 2, SKColorType.Rgba8888, SKAlphaType.Premul));
		Assert.IsNotNull(surface);
		surface.Canvas.Clear(SKColors.Black);
		context.Flush(submit: true);
		Assert.IsGreaterThan(0, gl.GeneratedTextures, "An owned GPU resource must exist before context loss.");
		Assert.IsGreaterThan(0, gl.Calls);
		Assert.AreEqual(0, gl.DeletedTextures);
		gl.ContextLost = true;

		if (releaseResourcesControl)
		{
			// Prove that this mock observes the unsafe alternative, including deletion of owned textures.
			context.AbandonContext(releaseResources: true);
			Assert.IsGreaterThan(0, gl.CallsAfterContextLoss);
			Assert.IsGreaterThan(0, gl.DeletedTextures);
		}
		else
		{
			factory.AbandonContext();
			Assert.AreEqual(0, gl.CallsAfterContextLoss, "Abandonment must use releaseResources: false semantics.");
			Assert.AreEqual(0, gl.DeletedTextures);
		}

		Assert.IsTrue(context.IsAbandoned);
		var callsAfterAbandonment = gl.Calls;
		surface.Dispose();
		factory.Dispose();
		Assert.AreEqual(callsAfterAbandonment, gl.Calls, "Disposing abandoned Skia resources must not call GL.");
	}

	[TestMethod]
	[DataRow(false, false)]
	[DataRow(false, true)]
	[DataRow(true, false)]
	[DataRow(true, true)]
	public void BackendIsAbandonedBeforeDisposalWhenMakeCurrentFails(bool makeCurrent, bool throwOnDispose)
	{
		var calls = new List<string>();
		var context = new FakeContext(calls, makeCurrent);
		var factory = new FakeFactory(calls, context, throwOnDispose);

		if (throwOnDispose)
		{
			Assert.ThrowsExactly<InvalidOperationException>(() => WaylandRenderer.ReleaseGraphics(context, factory));
		}
		else
		{
			WaylandRenderer.ReleaseGraphics(context, factory);
		}

		CollectionAssert.AreEqual(makeCurrent
			? new[] { "make-current", "dispose-factory", "dispose-context" }
			: new[] { "make-current", "abandon", "dispose-factory", "dispose-context" }, calls);
		Assert.AreEqual(0, factory.CallsIntoLostContext);
		Assert.AreEqual(!makeCurrent, factory.Abandoned);
	}

	private sealed class GlTarget : IGLRenderTarget
	{
		public int Width => 2;
		public int Height => 2;
		public GraphicsColorFormat ColorFormat => GraphicsColorFormat.Rgba8888;
		public uint FramebufferId => 0;
		public int SampleCount => 0;
		public int StencilBits => 8;
		public void Dispose()
		{
		}
	}

	private sealed class FakeContext(List<string> calls, bool makeCurrent) : IWaylandGraphicsContext, IWaylandGpuTeardownContext
	{
		public GraphicsContextKind Kind => GraphicsContextKind.OpenGLES;
		public bool Committed => false;
		public bool IsCurrent { get; private set; }
		public bool TryMakeCurrentForTeardown()
		{
			calls.Add("make-current");
			return IsCurrent = makeCurrent;
		}
		public IRenderTarget AcquireRenderTarget(int width, int height) => throw new NotSupportedException();
		public void Present() => throw new NotSupportedException();
		public void Dispose() => calls.Add("dispose-context");
	}

	private sealed class FakeFactory(List<string> calls, FakeContext context, bool throwOnDispose) : IDrawingFactory, IGpuDrawingFactory, IDisposable
	{
		public bool Abandoned { get; private set; }
		public int CallsIntoLostContext { get; private set; }
		public void AbandonContext()
		{
			calls.Add("abandon");
			Abandoned = true;
		}
		public void Dispose()
		{
			calls.Add("dispose-factory");
			if (!Abandoned && !context.IsCurrent)
			{
				CallsIntoLostContext++;
			}
			if (throwOnDispose)
			{
				throw new InvalidOperationException();
			}
		}
		public ITexture RenderOffscreen(int pixelWidth, int pixelHeight, Action<IDrawingSession> render) => throw new NotSupportedException();
		public Task<IImage> SnapshotAsync(ITexture texture) => throw new NotSupportedException();
		public ITexture CreateTexture(IImage image) => throw new NotSupportedException();
		public ITexture CreateTexture(int pixelWidth, int pixelHeight, ReadOnlySpan<byte> bgraPremul) => throw new NotSupportedException();
		public IShader CreateLinearGradientShader(Vector2 start, Vector2 end, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix) => throw new NotSupportedException();
		public IShader CreateRadialGradientShader(Vector2 center, Vector2 gradientOrigin, float radiusX, float radiusY, Color[] colors, float[] colorPositions, GradientTileMode tileMode, Matrix3x2 localMatrix) => throw new NotSupportedException();
		public IColorFilter CreateTintColorFilter(Color color) => throw new NotSupportedException();
		public IColorFilter CreateColorMatrixColorFilter(float[] matrix) => throw new NotSupportedException();
		public IEffectFilter? CreateEffectFilter(EffectNode tree, Rect bounds) => throw new NotSupportedException();
		public IEffectFilter CreateDropShadowFilter(float dx, float dy, float sigmaX, float sigmaY, Color color) => throw new NotSupportedException();
		public ICommandRecorder CreateRecording() => throw new NotSupportedException();
	}
}
