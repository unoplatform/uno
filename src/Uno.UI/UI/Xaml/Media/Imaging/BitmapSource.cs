using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Core;

namespace Windows.UI.Xaml.Media.Imaging
{
	public partial class BitmapSource : ImageSource
	{
		#region PixelHeight DependencyProperty

		public int PixelHeight
		{
			get { return (int)GetValue(PixelHeightProperty); }
			internal set { SetValue(PixelHeightProperty, value); }
		}

		// Using a DependencyProperty as the backing store for PixelHeight.  This enables animation, styling, binding, etc...
		public static DependencyProperty PixelHeightProperty { get ; } =
			DependencyProperty.Register("PixelHeight", typeof(int), typeof(BitmapSource), new FrameworkPropertyMetadata(0, (s, e) => ((BitmapSource)s)?.OnPixelHeightChanged(e)));

		private void OnPixelHeightChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region PixelWidth DependencyProperty

		public int PixelWidth
		{
			get { return (int)GetValue(PixelWidthProperty); }
			internal set { SetValue(PixelWidthProperty, value); }
		}

		// Using a DependencyProperty as the backing store for PixelWidth.  This enables animation, styling, binding, etc...
		public static DependencyProperty PixelWidthProperty { get ; } =
			DependencyProperty.Register("PixelWidth", typeof(int), typeof(BitmapSource), new FrameworkPropertyMetadata(0, (s, e) => ((BitmapSource)s)?.OnPixelWidthChanged(e)));


		private void OnPixelWidthChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

#if __NETSTD__
		protected IRandomAccessStream _stream;
#endif

		protected BitmapSource() { }

		protected BitmapSource(Uri sourceUri) : base(sourceUri)
		{

		}

		protected BitmapSource(string sourceString) : base(sourceString)
		{

		}

		/// <summary>
		/// Helper for Uno... not part of UWP contract
		/// </summary>
		public void SetSource(Stream streamSource)
			=> SetSourceCore(streamSource.AsRandomAccessStream());

		/// <summary>
		/// Helper for Uno... not part of UWP contract
		/// </summary>
		public Task SetSourceAsync(Stream streamSource)
		{
			SetSourceCore(streamSource.AsRandomAccessStream());
			return ForceLoad(CancellationToken.None);
		}

		public void SetSource(IRandomAccessStream streamSource)
			=> SetSourceCore(streamSource);

		public IAsyncAction SetSourceAsync(IRandomAccessStream streamSource)
		{
			SetSourceCore(streamSource);
			return AsyncAction.FromTask(ForceLoad);
		}

		private void SetSourceCore(IRandomAccessStream streamSource)
		{
			if (streamSource == null)
			{
				//Same behavior as windows, although the documentation does not mention it!!!
				throw new ArgumentException(nameof(streamSource));
			}

			PixelWidth = 0;
			PixelHeight = 0;

			// The source has to be cloned before leaving the "SetSource[Async]".
			var clonedStreamSource = streamSource.CloneStream();

#if __NETSTD__
			_stream = clonedStreamSource;
#else
			Stream = clonedStreamSource.AsStream();
#endif
		}

		private async Task ForceLoad(CancellationToken ct)
		{
#if __NETSTD__
			var tcs = new TaskCompletionSource<object>();
			using var r = ct.Register(() => tcs.TrySetCanceled());
			using var s = Subscribe(OnChanged);

			InvalidateSource();

			await tcs.Task;

			void OnChanged(ImageData data)
			{
				tcs.TrySetResult(null);
			}
#endif
		}

		public override string ToString()
		{
			if (WebUri is { } uri)
			{
				return $"{GetType().Name}/{uri}";
			}

#if __NETSTD__
			if (_stream is { } stream)
			{
				return $"{GetType().Name}/{stream.GetType()}";
			}
#else
			if (Stream is { } stream)
			{
				return $"{GetType().Name}/{stream.GetType()}";
			}
#endif

			return $"{GetType().Name}/-empty-";
		}
	}
}
