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
		{
			SetSource(streamSource.AsRandomAccessStream());
		}

		/// <summary>
		/// Helper for Uno... not part of UWP contract
		/// </summary>
		public async Task SetSourceAsync(Stream streamSource)
			=> await SetSourceAsync(streamSource.AsRandomAccessStream());

		public void SetSource(IRandomAccessStream streamSource)
			// We prefer to use the SetSourceAsync here in order to make sure that the stream is copied ASYNChronously,
			// which is important since we are using a stream wrapper of and <In|Out|RA>Stream which might freeze the UI thread / throw exception.
			=> Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SetSourceAsync(streamSource));

		public IAsyncAction SetSourceAsync(IRandomAccessStream streamSource)
		{
			async Task SetSourceAsync(CancellationToken ct)
			{
				if (streamSource == null)
				{
					//Same behavior as windows, although the documentation does not mention it!!!
					throw new ArgumentException(nameof(streamSource));
				}

				PixelWidth = 0;
				PixelHeight = 0;

#if __NETSTD__
				_stream = streamSource.CloneStream();

				var tcs = new TaskCompletionSource<object>();

				using var x = Subscribe(OnChanged);

				InvalidateSource();

				await tcs.Task;

				void OnChanged(ImageData data)
				{
					tcs.TrySetResult(null);
				}
#else
				Stream = streamSource.CloneStream().AsStream();
#endif
			}

			return AsyncAction.FromTask(SetSourceAsync);

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
