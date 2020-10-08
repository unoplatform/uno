using System;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Storage.Streams;
using Uno.Foundation;

namespace Windows.UI.Xaml.Media.Imaging
{
	partial class WriteableBitmap
	{
		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, out ImageData image)
		{
			var handle = GCHandle.Alloc(_buffer.ToArray(), GCHandleType.Pinned);
			var pinnedData = handle.AddrOfPinnedObject();

			try
			{
				var value = WebAssemblyRuntime.InvokeJS("Uno.UI.WindowManager.current.rawPixelsToBase64EncodeImage(" + pinnedData + ", " + PixelWidth + ", " + PixelHeight + ");");

				image = new ImageData
				{
					Kind = ImageDataKind.Base64,
					Value = value
				};
				return true;
			}
			finally
			{
				handle.Free();
			}
		}
	}
}
