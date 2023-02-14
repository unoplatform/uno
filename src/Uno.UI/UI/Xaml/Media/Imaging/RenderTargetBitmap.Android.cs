#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Android.Graphics;
using Uno.UI;
using Windows.Foundation;
using Java.Nio;
using Android.Views;
using Uno.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Media.Imaging
{
	partial class RenderTargetBitmap
	{
		/// <summary>
		/// Forces all descendent views to render using the software renderer instead
		/// of the hardware renderer.
		/// </summary>
		/// <remarks>
		/// This allows for harware-only surfaces, like overlays, to be visible in the 
		/// the screenshots.
		/// </remarks>
		private static void SetSoftwareRendering(View nativeView, bool enabled)
		{
			var layerType = enabled ? LayerType.Software : LayerType.Hardware;

			nativeView.SetLayerType(layerType, null);

			if (nativeView is ViewGroup viewGroup)
			{
				foreach (var child in viewGroup.EnumerateAllChildren(maxDepth: 1024))
				{
					child.SetLayerType(layerType, null);
				}
			}
		}
		/// <inheritdoc />
		private protected override bool IsSourceReady => _buffer != null;

		/// <inheritdoc />
		private protected override bool TryOpenSourceSync(int? targetWidth, int? targetHeight, [NotNullWhen(true)] out ImageData image)
		{
			image = default;
			if (_buffer is null)
			{
				return false;
			}
			image = ImageData.FromBitmap(BitmapFactory.DecodeByteArray(_buffer, 0, _bufferSize));
			return image.HasData;
		}

		private (int ByteCount, int Width, int Height) RenderAsBgra8_Premul(UIElement element, ref byte[]? buffer, Size? scaledSize = null)
		{

			var byteCount = 0;
			Bitmap? bitmap = default;

			// In UWP RenderTargetBitmap use logical size
			// if using logical size to render the element there are generate glycth.
			// to avoid this layout the control on Physical and after scale to logical
			var logical = element.ActualSize.ToSize();
			var physical = logical.LogicalToPhysicalPixels();
			if (physical.IsEmpty)
			{
				return (0, 0, 0);
			}
			try
			{
				SetSoftwareRendering(element, true);
				bitmap = Bitmap.CreateBitmap((int)physical.Width, (int)physical.Height, Bitmap.Config.Argb8888!, true)
					?? throw new InvalidOperationException("Failed to create target native bitmap.");
				using var canvas = new Canvas(bitmap);
				{
					// Make sure the element has been Layouted.
					Uno.UI.UnoViewGroup.TryFastRequestLayout(element, false);
					// Render on the canvas
					canvas.DrawColor(Colors.Transparent, PorterDuff.Mode.Clear!);
					element.Draw(canvas);
				}

				var targetSize = scaledSize ?? logical;
				if (targetSize is { } && targetSize != physical)
				{
					using var oldbitmap = bitmap;
					bitmap = Bitmap.CreateScaledBitmap(oldbitmap, (int)targetSize.Width, (int)targetSize.Height, false)
						?? throw new InvalidOperationException("Failed to scaled native bitmap to the requested size.");
				}

				byteCount = bitmap.ByteCount;
				using var byteArray = ByteBuffer.Allocate(byteCount);
				bitmap.CopyPixelsToBuffer(byteArray);
				if (byteArray is null)
				{
					return (0, 0, 0);
				}

				EnsureBuffer(ref buffer, byteCount);

				byteArray.Rewind();
				//This is called for ensure correct byte order
				var ordered = byteArray.Order(ByteOrder.BigEndian!);
				ordered.Get(buffer!, 0, byteCount);
				//Android store Argb8888 as rgba8888
				SwapRB(ref buffer!, byteCount);
				return (byteCount, bitmap.Width, bitmap.Height);
			}
			finally
			{
				bitmap?.Dispose();
				SetSoftwareRendering(element, false);
			}


		}
	}
}
