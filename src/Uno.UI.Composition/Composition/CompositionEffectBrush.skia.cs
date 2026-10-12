#nullable enable

using System;
using System.Numerics;
using Windows.Foundation;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.Effects;

namespace Microsoft.UI.Composition;

public partial class CompositionEffectBrush : CompositionBrush
{
	private Rect _currentBounds;
	private Vector2 _currentScale;
	// The factory that minted _filter and the tree's textures. They are device-bound, so a re-bound renderer
	// (DrawingFactory.Current swapped after a context loss) invalidates them even at identical bounds/scale.
	private IDrawingFactory? _currentFactory;
	private IEffectFilter? _filter;
	private EffectNode? _tree;
	private bool _hasBackdropBrushInput;

	internal bool HasBackdropBrushInput
	{
		get => _hasBackdropBrushInput;
		private set => SetProperty(ref _hasBackdropBrushInput, value);
	}

	internal override bool RequiresRepaintOnEveryFrame => HasBackdropBrushInput;

	private float _backdropBlurSigma;

	internal override float DamageRegionSamplingMargin => HasBackdropBrushInput ? _backdropBlurSigma * 3f : 0f;

	internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
	{
		UpdateFilter(session.Factory, bounds, GetRasterizationScale(session));
		if (_filter is { } filter)
		{
			session.DrawEffectBackdrop(filter, opacity);
			return true;
		}
		// The backend couldn't realize the effect as a filter (e.g. the WebGPU backend for a per-pixel colour
		// effect). Fall back to the neutral "recipe": reduce the graph to a single source + composed 4×5 colour
		// matrix and paint the source with that matrix applied.
		if (TryGetWebGpuEffectRecipe(out var source, out _, out var solidColor, out var matrix))
		{
			if (solidColor is { } sc)
			{
				session.DrawRect(bounds, ApplyColorMatrix(sc, matrix));
			}
			else if (source is { } src)
			{
				using var recipeFilter = session.Factory.CreateColorMatrixColorFilter(matrix);
				var count = session.Save();
				session.SaveLayer(recipeFilter);
				src.TryPaint(session, opacity, bounds);
				session.RestoreToCount(count);
			}
		}
		return true;
	}

	// Set by PrepareForOffscreenRasterization, consumed by the very next UpdateFilter: "the graph was just parsed
	// outside the caller's offscreen; don't re-parse (which would re-rasterize sources INSIDE that offscreen and
	// re-enter RenderOffscreen)". One-shot so per-frame re-parse of dynamic sources is otherwise unchanged.
	private bool _preparedThisPass;

	// Pre-parse the effect graph (rasterizing any nested-brush sources) BEFORE this brush is painted into a caller's
	// offscreen, so those nested RenderOffscreen passes run sequentially rather than re-entering the outer one.
	// Re-entrant offscreen rendering isn't contractual and can corrupt a backend's per-pass scratch. Prepares only
	// when the paint that follows would actually rebuild the graph, so a per-frame caller (a nine-grid over an effect
	// source) doesn't re-rasterize every source texture on every frame.
	internal override void PrepareForOffscreenRasterization(IDrawingFactory factory, Rect bounds, Vector2 scale)
	{
		if (IsGraphCurrent(factory, bounds, scale))
		{
			return;
		}

		ParseGraph(factory, bounds, scale);
		_preparedThisPass = true;
	}

	// The cached graph is reusable only for the same region, at the same resolution, on the same backend device.
	private bool IsGraphCurrent(IDrawingFactory factory, Rect bounds, Vector2 scale)
		=> _filter is not null && _currentBounds == bounds && _currentScale == scale && ReferenceEquals(_currentFactory, factory);

	private void UpdateFilter(IDrawingFactory factory, Rect bounds, Vector2 scale)
	{
		if (_preparedThisPass && _currentBounds == bounds && _currentScale == scale && ReferenceEquals(_currentFactory, factory))
		{
			_preparedThisPass = false;   // consume: reuse the graph PrepareForOffscreenRasterization just parsed
			return;
		}

		if (IsGraphCurrent(factory, bounds, scale))
		{
			return;
		}

		ParseGraph(factory, bounds, scale);
	}

	private void ParseGraph(IDrawingFactory factory, Rect bounds, Vector2 scale)
	{
		if (_currentFactory is not null && !ReferenceEquals(_currentFactory, factory))
		{
			// The renderer was re-bound: these handles belong to a device that is gone, so drop them rather than
			// calling into it to dispose them.
			_tree = null;
			_filter = null;
		}

		DisposeTree();
		_filter?.Dispose();

		// Parse the D2D graph once into a neutral tree (brush inputs rasterized to textures, backdrop left as a
		// deferred leaf), then have the backend fuse it into one filter. hasBackdrop is a tree property, computed
		// here — not reported by the backend. A null filter means the backend can't realize it (e.g. WebGPU for a
		// per-pixel colour effect); TryPaint then falls back to the recipe path. Not an error.
		_tree = EffectGraphParser.Parse(_effect, bounds, scale, GetSourceParameter, factory);
		_filter = factory.CreateEffectFilter(_tree, bounds);
		_backdropBlurSigma = GetMaxBlurSigma(_tree);
		HasBackdropBrushInput = _tree.ContainsSourceInput();
		_currentBounds = bounds;
		_currentScale = scale;
		_currentFactory = factory;
	}

	private static float GetMaxBlurSigma(EffectNode node)
	{
		var sigma = node is BlurEffectNode blur ? blur.Sigma : 0f;
		foreach (var child in node.Children)
		{
			sigma = Math.Max(sigma, GetMaxBlurSigma(child));
		}

		return sigma;
	}

	private void DisposeTree()
	{
		if (_tree is { } tree)
		{
			foreach (var texture in tree.EnumerateTextures())
			{
				texture.Texture.Dispose();
			}

			_tree = null;
		}
	}

	private protected override void DisposeInternal()
	{
		base.DisposeInternal();

		DisposeTree();
		_filter?.Dispose();
	}

	internal override bool CanPaint() => _effect is not null;
}
