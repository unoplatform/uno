using System;
using System.Collections.Generic;
using Windows.Graphics.Effects;

namespace Windows.UI.Composition
{
	public partial class CompositionEffectFactory : CompositionObject
	{
		private IGraphicsEffect _effect;
		private IEnumerable<string> _animatableProperties;

		private CompositionEffectFactoryLoadStatus _loadStatus;
		private Exception _extendedError;

		internal CompositionEffectFactory(IGraphicsEffect effect, IEnumerable<string> animatableProperties = null)
		{
			if (effect is null)
				throw new ArgumentNullException(nameof(effect));

			_effect = effect;
			_animatableProperties = animatableProperties;
		}

		public CompositionEffectBrush CreateBrush() => new(_effect, _animatableProperties);

		public CompositionEffectFactoryLoadStatus LoadStatus => _loadStatus;
		public Exception ExtendedError => _extendedError;
	}
}
