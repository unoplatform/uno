#nullable enable

using Microsoft.UI.Text;
using SkiaSharp;

namespace Microsoft.UI.Xaml.Documents;

internal sealed class InlineObjectInfo
{
	internal InlineObjectInfo(SKImage? image, float width, float height, float ascent, VerticalCharacterAlignment verticalAlignment)
	{
		Image = image;
		Width = width;
		Height = height;
		Ascent = ascent;
		VerticalAlignment = verticalAlignment;
	}

	internal SKImage? Image { get; }

	internal float Width { get; }

	internal float Height { get; }

	internal float Ascent { get; }

	internal VerticalCharacterAlignment VerticalAlignment { get; }
}
