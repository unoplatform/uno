using System;
using System.Linq;
using Windows.UI.Composition;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// Represents the raw data of an **opened** image source
	/// </summary>
	internal partial struct ImageData
	{
		public ImageDataKind Kind { get; set; }

		public Exception Error { get; set; }

#if __WASM__
		internal ImageSource Source { get; set; }

		public string Value { get; set; }
#elif __SKIA__
		public SkiaCompositionSurface Value { get; set; }
#endif
		public override string ToString() =>
			Kind switch
			{
				ImageDataKind.Empty => "Empty",
				ImageDataKind.Error => $"Error[{Error}]",
#if __WASM__ || __SKIA__
				_ => $"{Kind}: {Value}"
#else
				_ => $"{Kind}"
#endif
			};
	}
}
