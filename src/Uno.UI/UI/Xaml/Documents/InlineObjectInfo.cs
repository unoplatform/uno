#nullable enable

using Microsoft.UI.Text;
using Uno.UI.Composition.Drawing;

namespace Microsoft.UI.Xaml.Documents;

internal sealed class InlineObjectInfo
{
	internal InlineObjectInfo(IImage? image, object? imageKey, float width, float height, float ascent, VerticalCharacterAlignment verticalAlignment)
	{
		Image = image;
		ImageKey = imageKey;
		Width = width;
		Height = height;
		Ascent = ascent;
		VerticalAlignment = verticalAlignment;
	}

	internal IImage? Image { get; }

	/// <summary>Identifies the image's pixels across re-decodes, so they can share one texture.</summary>
	internal object? ImageKey { get; }

	internal float Width { get; }

	internal float Height { get; }

	internal float Ascent { get; }

	internal VerticalCharacterAlignment VerticalAlignment { get; }
}
