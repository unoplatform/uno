#nullable enable

using System;
using Windows.UI.Text;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// A font file (an embedded/URI font) loaded once by <see cref="IFontProvider.LoadFontFile"/>, from which fonts of any
/// size/style are built without reading the file again.
/// </summary>
public interface IFontFile : IDisposable
{
	/// <summary>Same contract as <see cref="IFontProvider.CreateFont"/>, over this file's contents.</summary>
	IFont? CreateFont(string? familyNameHint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize);
}
