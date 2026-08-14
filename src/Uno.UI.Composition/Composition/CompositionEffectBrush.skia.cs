#nullable enable

using System;
using Windows.Foundation;
using Uno.UI.Composition.Drawing;
using Uno.UI.Composition.Effects;

namespace Microsoft.UI.Composition;

public partial class CompositionEffectBrush : CompositionBrush
{
	// The neutral fused-tree path is the default: Uno parses the D2D graph into an EffectNode tree and the backend
	// fuses it. A graph containing an effect Uno hasn't neutralized yet (an UnsupportedEffectNode) transparently
	// falls back to the legacy per-backend realization, so nothing regresses. UNO_EFFECT_TREE=0 forces the legacy
	// path for the whole brush (escape hatch).
	private static readonly bool _forceLegacy = Environment.GetEnvironmentVariable("UNO_EFFECT_TREE") is "0";

	private Rect _currentBounds;
	private IEffectFilter? _filter;
	private EffectNode? _tree;
	private bool _hasBackdropBrushInput;

	internal bool HasBackdropBrushInput
	{
		get => _hasBackdropBrushInput;
		private set => SetProperty(ref _hasBackdropBrushInput, value);
	}

	internal override bool RequiresRepaintOnEveryFrame => HasBackdropBrushInput;

	internal override bool TryPaint(IDrawingSession session, float opacity, Rect bounds)
	{
		UpdateFilter(bounds);
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
				var count = session.Save();
				session.SaveLayer(DrawingFactory.Current.CreateColorMatrixColorFilter(matrix));
				src.TryPaint(session, opacity, bounds);
				session.RestoreToCount(count);
			}
		}
		return true;
	}

	private void UpdateFilter(Rect bounds)
	{
		if (_currentBounds == bounds && _filter is not null)
		{
			return;
		}

		DisposeTree();
		_filter?.Dispose();

		if (!_forceLegacy)
		{
			// Parse the D2D graph once into a neutral tree (brush inputs rasterized to textures, backdrop left as a
			// deferred leaf). A fully-neutralized graph is fused by the backend; a graph containing an effect we
			// don't translate yet falls back to the legacy realization so nothing regresses. hasBackdrop is a tree
			// property, computed here — not reported by the backend.
			_tree = EffectGraphParser.Parse(_effect, bounds, name => CompositionBrushEffectSource.From(GetSourceParameter(name)));
			if (!_tree.ContainsUnsupported())
			{
				_filter = DrawingFactory.Current.CreateEffectFilter(_tree, bounds);
				HasBackdropBrushInput = _tree.ContainsBackdrop();
				_currentBounds = bounds;
				return;
			}

			// Unsupported effect present — discard the (unused) rasterized textures and take the legacy path.
			DisposeTree();
		}

		// A null filter means the backend can't realize this effect as a backdrop filter — TryPaint falls back to
		// the recipe path (a source + composed colour matrix). Not an error.
		_filter = DrawingFactory.Current.CreateEffectFilter(
			_effect,
			bounds,
			name => CompositionBrushEffectSource.From(GetSourceParameter(name)),
			out var hasBackdropInput);
		HasBackdropBrushInput = hasBackdropInput;

		_currentBounds = bounds;
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
