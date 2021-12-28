#if !__IOS__ && !__ANDROID__
#define NOT_IMPLEMENTED
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Uno.Extensions;
using Uno.Foundation.Logging;
using Buffer = Windows.Storage.Streams.Buffer;

namespace Windows.UI.Xaml.Media.Imaging
{
#if NOT_IMPLEMENTED
	[global::Uno.NotImplemented()]
#endif
	public partial class RenderTargetBitmap
	{
#if NOT_IMPLEMENTED
		internal const bool IsImplemented = false;
#else
		internal const bool IsImplemented = true;
#endif

		#region PixelWidth
#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented()]
#endif
		public static DependencyProperty PixelWidthProperty { get; } = DependencyProperty.Register(
			"PixelWidth", typeof(int), typeof(RenderTargetBitmap), new FrameworkPropertyMetadata(default(int)));

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented()]
#endif
		public int PixelWidth
		{
			get => (int)GetValue(PixelWidthProperty);
			private set => SetValue(PixelWidthProperty, value);
		}
		#endregion

		#region PixelHeight

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented()]
#endif
		public static DependencyProperty PixelHeightProperty { get; } = DependencyProperty.Register(
			"PixelHeight", typeof(int), typeof(RenderTargetBitmap), new FrameworkPropertyMetadata(default(int)));

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented()]
#endif
		public int PixelHeight
		{
			get => (int)GetValue(PixelHeightProperty);
			private set => SetValue(PixelHeightProperty, value);
		} 
		#endregion

		private byte[] _buffer;

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented()]
#endif
		public IAsyncAction RenderAsync(UIElement element, int scaledWidth, int scaledHeight)
			=> AsyncAction.FromTask(async ct =>
			{
				try
				{
					_buffer = RenderAsPng(element, new Size(scaledWidth, scaledHeight));

					PixelWidth = scaledWidth;
					PixelHeight = scaledHeight;

#if __WASM__ || __SKIA__
					InvalidateSource();
#endif
				}
				catch (Exception error)
				{
					this.Log().Error("Failed to render element to bitmap.", error);
				}
			});

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented()]
#endif
		public IAsyncAction RenderAsync(UIElement element)
			=> AsyncAction.FromTask(async ct =>
			{
				try
				{
					_buffer = RenderAsPng(element);

					PixelWidth = (int)element.ActualSize.X;
					PixelHeight = (int)element.ActualSize.Y;

#if __WASM__ || __SKIA__
					InvalidateSource();
#endif
				}
				catch (Exception error)
				{
					this.Log().Error("Failed to render element to bitmap.", error);
				}
			});

#if NOT_IMPLEMENTED
		[global::Uno.NotImplemented()]
#endif
		public IAsyncOperation<IBuffer> GetPixelsAsync()
			=> AsyncOperation<IBuffer>.FromTask(async (op, ct) =>
			{
				return new Buffer(_buffer);
			});

#if NOT_IMPLEMENTED
		private static byte[] RenderAsPng(UIElement element, Size? scaledSize = null)
			=> throw new NotImplementedException("RenderTargetBitmap is not supported on this platform.");
#endif
	}
}
