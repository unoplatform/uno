using System.Numerics;

namespace Microsoft.UI.Composition;

internal static class VisualExtensions
{
	internal static Vector3 GetTotalOffset(this Visual visual)
	{
		if (visual.IsTranslationEnabled && visual.Properties.TryGetVector3("Translation", out var translation) == CompositionGetValueStatus.Succeeded)
		{
			return visual.Offset + translation;
		}

		return visual.Offset;
	}
}
