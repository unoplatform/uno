#nullable enable

using Windows.Graphics.Effects;
using System.Collections.Generic;

namespace Windows.UI.Composition
{
	public partial class CompositionEffectBrush : CompositionBrush
	{
		private IGraphicsEffect _effect;
		//private IEnumerable<string> _animatableProperties; // TODO: Uncomment and implement this when we implement Composition animations

		private Dictionary<string, CompositionBrush> _sourceParameters;

		internal CompositionEffectBrush(IGraphicsEffect graphicsEffect, IEnumerable<string>? animatableProperties = null)
		{
			_effect = graphicsEffect;
			//_animatableProperties = animatableProperties; // TODO (see the TODO note above)

			_sourceParameters = new();
		}

		public CompositionBrush? GetSourceParameter(string name)
		{
			if (_sourceParameters.TryGetValue(name, out var result))
				return result;

			return null;
		}

		public void SetSourceParameter(string name, CompositionBrush source) => _sourceParameters[name] = source;
	}
}
