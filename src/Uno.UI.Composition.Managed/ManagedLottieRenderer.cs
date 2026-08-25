#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The framework's SkiaSharp-free managed Lottie (Bodymovin) engine — an <see cref="ILottieRenderer"/> that parses
/// the animation JSON and draws each frame straight through the neutral <see cref="IDrawingSession"/> (no Skottie,
/// no rasterize-to-SKSurface), so Lottie plays on any backend. Resolved as the default only when the Skottie add-in
/// (Uno.UI.Lottie) isn't referenced, or forced with <c>UNO_MANAGED_LOTTIE=1</c>. v1 covers the shape-layer subset
/// (see <see cref="ManagedLottie"/>).
/// </summary>
public sealed class ManagedLottieRenderer : ILottieRenderer
{
	// Reflective bootstrap entry point (found by name from UnoPlatformHostBuilder); keep the type/method name stable.
	internal static ILottieRenderer CreateLottieRenderer() => new ManagedLottieRenderer();

	public ILottieAnimation? Load(string animationJson, IGeometryFactory geometry)
		=> ManagedLottie.TryParse(animationJson, out var model) && model is not null
			? new ManagedLottieAnimation(model, geometry)
			: null;

	private sealed class ManagedLottieAnimation : ILottieAnimation
	{
		private readonly ManagedLottie _model;
		private readonly IGeometryFactory _geometry;

		public ManagedLottieAnimation(ManagedLottie model, IGeometryFactory geometry)
		{
			_model = model;
			_geometry = geometry;
		}

		public Vector2 Size => _model.Size;

		public TimeSpan Duration => _model.Duration;

		public void Render(IDrawingSession session, float progress, Rect area)
			=> _model.Render(session, _geometry, progress, area);

		public void Dispose() { }
	}
}
