#nullable enable

using System.Collections.Generic;

namespace Microsoft.UI.Composition
{
	public partial class ScalarKeyFrameAnimation : global::Microsoft.UI.Composition.KeyFrameAnimation
	{
		private List<(float normalizedProgressKey, float value)> _keys = new List<(float normalizedProgressKey, float value)>();

		internal ScalarKeyFrameAnimation(Compositor compositor) : base(compositor)
		{
		}

		public override void InsertKeyFrame(float normalizedProgressKey, float value) => base.InsertKeyFrame(normalizedProgressKey, value);

		public override void InsertKeyFrame(float normalizedProgressKey, float value, CompositionEasingFunction easingFunction) => base.InsertKeyFrame(normalizedProgressKey, value, easingFunction);
	}
}
