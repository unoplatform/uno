#nullable enable

using System.IO;
using Windows.UI.Text;

namespace Uno.UI.Composition.Drawing;

/// <summary>
/// The default <see cref="IFontFile"/>: the file's bytes in one managed array, built through
/// <see cref="IFontProvider.CreateFont"/>.
/// </summary>
internal sealed class ByteArrayFontFile : IFontFile
{
	private readonly IFontProvider _provider;
	private readonly byte[] _data;

	private ByteArrayFontFile(IFontProvider provider, byte[] data)
	{
		_provider = provider;
		_data = data;
	}

	public static ByteArrayFontFile Load(IFontProvider provider, Stream stream) => new(provider, ReadAllBytes(stream));

	public IFont? CreateFont(string? familyNameHint, FontWeight weight, FontStretch stretch, FontStyle style, float fontSize)
		=> _provider.CreateFont(_data, familyNameHint, weight, stretch, style, fontSize);

	public void Dispose()
	{
	}

	private static byte[] ReadAllBytes(Stream stream)
	{
		if (stream is MemoryStream ms && ms.TryGetBuffer(out var seg) && seg.Offset == 0 && seg.Count == seg.Array!.Length)
		{
			return seg.Array;
		}

		// A known length allocates the array once; growing a MemoryStream by doubling leaves a trail of
		// large-object-heap buffers behind and then copies the whole file once more.
		if (stream.CanSeek)
		{
			var data = new byte[checked((int)(stream.Length - stream.Position))];
			stream.ReadExactly(data);
			return data;
		}

		using var copy = new MemoryStream();
		stream.CopyTo(copy);
		return copy.ToArray();
	}
}
