#nullable enable

using System.IO;
using System.Threading.Tasks;
using Windows.UI.Text;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// Backend font resolver: turns a family/style request (or raw font bytes, or a codepoint) into an
/// <see cref="IFont"/> handle. Obtained from <see cref="FontProvider.Current"/>. Byte loading for
/// application/URI fonts stays with the caller (it needs the app's storage APIs); this only turns bytes into a handle.
/// </summary>
public interface IFontProvider
{
	/// <summary>Builds a font from raw sfnt bytes (an embedded/URI font), selecting <paramref name="familyNameHint"/> within a collection and positioning any variable axes for the requested style. Returns <c>null</c> if the bytes aren't a usable font.</summary>
	IFont? CreateFont(byte[] data, string? familyNameHint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize);

	/// <summary>
	/// Reads a font file once into storage that every font built from it (one per size/style) shares. The default keeps
	/// the bytes in a managed array and builds through <see cref="CreateFont"/>; a native backend reads straight into
	/// its own memory instead.
	/// </summary>
	IFontFile LoadFontFile(Stream stream) => ByteArrayFontFile.Load(this, stream);

	/// <summary>Resolves an installed font family, or <c>null</c> if the family is unknown (caller falls back to the default).</summary>
	IFont? MatchFamily(string familyName, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize);

	/// <summary>
	/// Finds a font that can render <paramref name="codepoint"/> when the requested family can't, or <c>null</c> if none.
	/// Asynchronous because fallback may fetch a font on demand (e.g. the browser downloading a Noto font); an installed
	/// match completes synchronously (callers can consume the completed <see cref="ValueTask{T}"/> without awaiting).
	/// This is the sole fallback seam — a custom provider implements it to supply its own fallback.
	/// </summary>
	ValueTask<IFont?> MatchCharacterAsync(int codepoint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize);

	/// <summary>Returns a guaranteed-usable default font for the requested style.</summary>
	IFont GetDefaultFont(FontWeight weight, FontStretch stretch, FontStyle style, float fontSize);
}
