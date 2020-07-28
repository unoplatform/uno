#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
using System.Collections.Generic;

namespace Windows.UI.Composition
{
	public partial class ScalarKeyFrameAnimation : global::Windows.UI.Composition.KeyFrameAnimation
	{
		private List<(float normalizedProgressKey, float value)> _keys = new List<(float normalizedProgressKey, float value)>();

		internal ScalarKeyFrameAnimation(Compositor compositor) : base(compositor)
		{
		}

		public override void InsertKeyFrame(float normalizedProgressKey, float value) => base.InsertKeyFrame(normalizedProgressKey, value);

		public override void InsertKeyFrame(float normalizedProgressKey, float value, CompositionEasingFunction easingFunction) => base.InsertKeyFrame(normalizedProgressKey, value, easingFunction);
	}
}
