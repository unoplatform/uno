#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Uno.Diagnostics.Eventing;
using Uno.Disposables;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Uno.UI.Xaml.Media;
using Windows.UI.Core;

#if __IOS__
using UIKit;
#else
using AppKit;
#endif

namespace Windows.UI.Xaml.Media;

public partial class ImageSource
{
	private readonly bool _isOriginalSourceUIImage;
	private static readonly IEventProvider _trace = Tracing.Get(TraceProvider.Id);

	partial void InitFromResource(Uri uri)
	{
		var path = uri
			.PathAndQuery
			.TrimStart('/')

			// UWP supports backward slash in path for directory separators.
			.Replace("\\", "/");

		BundlePath = path;

		BundleName = uri != null
			? Path.GetFileName(uri.AbsolutePath)
			: null;
	}

	partial void CleanupResource()
	{
		BundlePath = null;
		BundleName = null;
	}

	partial void DisposePartial()
	{
		DisposeNativeImageData();

		_imageData = ImageData.Empty;
	}

	/// <summary>
	/// Similar to Dispose, but the ImageSource can still be used in the future.
	/// </summary>
	partial void UnloadImageDataPlatform()
	{
		// If the original source is a NSImage, we can't dispose it because we will
		// not be able to restore it later (from a AbsoluteUri, BundleName, file path, etc.)
		if (!_isOriginalSourceUIImage)
		{
			DisposeNativeImageData();
		}
	}

	private void DisposeNativeImageData()
	{
		if (_imageData.Kind == ImageDataKind.NativeImage)
		{
			_imageData.NativeImage?.Dispose();
		}
	}

	public bool HasSource()
	{
		return IsSourceReady
			|| Stream != null
			|| AbsoluteUri != null
			|| !FilePath.IsNullOrWhiteSpace()
			|| _imageData.HasData
			|| HasBundle;
	}

	/// <summary>
	/// Determines if the current instance references a local bundle resource.
	/// </summary>
	public bool HasBundle => !BundlePath.IsNullOrWhiteSpace() || !BundleName.IsNullOrWhiteSpace();

	/// <summary>
	/// Open bundle is using either the name of the bundle (for
	/// android compatibility), or the path to the bundle for Windows compatibility.
	/// </summary>
	internal ImageData OpenBundle()
	{
		var image = OpenBundleFromString(BundleName) ?? OpenBundleFromString(BundlePath);

		if (image is null)
		{
			this.Log().ErrorFormat("Unable to locate bundle resource [{0}]", BundleName ?? BundlePath ?? "");

			return _imageData = ImageData.Empty;
		}

		return _imageData = ImageData.FromNative(image);
	}

	/// <summary>
	/// Indicates that this ImageSource has enough information to be opened
	/// </summary>
	private protected virtual bool IsSourceReady => false;

	/// <summary>
	/// Retrieves the already loaded image, or for supported source (eg. WriteableBitmap, cf remarks),
	/// create a native image from the data in memory.
	/// </summary>
	/// <remarks>
	/// This is only intended to convert **uncompressed data already in memory**,
	/// and should not be used to decompress a JPEG for instance, even if the already in memory.
	/// </remarks>
	internal bool TryOpenSync(out ImageData image)
	{
		if (_imageData.HasData)
		{
			image = _imageData;
			return true;
		}

		if (IsSourceReady && TryOpenSourceSync(null, null, out image))
		{
			return true;
		}

		image = default;
		return false;
	}

	internal async Task<ImageData> Open(CancellationToken ct)
	{
		using (
		   _trace.WriteEventActivity(
			   TraceProvider.ImageSource_SetImageDecodeStart,
			   TraceProvider.ImageSource_SetImageDecodeStop,
			   new object[] { this.GetDependencyObjectId() }
		   )
		)
		{
			if (ct.IsCancellationRequested)
			{
				return ImageData.Empty;
			}

			if (IsSourceReady && TryOpenSourceSync(null, null, out var img))
			{
				return _imageData = img;
			}

			if (IsSourceReady && TryOpenSourceAsync(ct, null, null, out var asyncImg))
			{
				return _imageData = await asyncImg;
			}

			if (Stream is not null)
			{
				return OpenImageDataFromStream();
			}

			if (!FilePath.IsNullOrEmpty())
			{
				return await OpenImageDataFromFilePathAsync();
			}

			if (HasBundle)
			{
				return await OpenImageDataFromBundleAsync(ct);
			}

			return await DownloadAndOpenImageDataAsync(ct);
		}
	}

	public override string ToString()
	{
		var source = Stream ?? AbsoluteUri ?? FilePath ?? (object)_imageData ?? BundlePath ?? BundleName ?? "[No source]";
		return "ImageSource: {0}".InvariantCultureFormat(source);
	}
}
