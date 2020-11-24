using System;
using System.IO;
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

		protected BitmapSource() { }

		protected BitmapSource(Uri sourceUri) : base(sourceUri)
		{

		}

		protected BitmapSource(string sourceString) : base(sourceString)
		{

		}

		public void SetSource(Stream streamSource)
		{
			PixelWidth = 0;
			PixelHeight = 0;

			var copy = new MemoryStream();
			streamSource.CopyTo(copy);
			copy.Position = 0;
			Stream = copy;

#if NETSTANDARD
			InvalidateSource();
#endif
		}

		public async Task SetSourceAsync(Stream streamSource)
		{
			if (streamSource != null)
			{
				PixelWidth = 0;
				PixelHeight = 0;

				var copy = new MemoryStream();
				await streamSource.CopyToAsync(copy);
				copy.Position = 0;
				Stream = copy;

#if NETSTANDARD
				InvalidateSource();
#endif
			}
			else
			{
				//Same behavior as windows, although the documentation does not mention it!!!
				throw new ArgumentException(nameof(streamSource));
			}
		}

		public void SetSource(IRandomAccessStream streamSource)
			// We prefer to use the SetSourceAsync here in order to make sure that the stream is copied ASYNChronously,
			// which is important since we are using a stream wrapper of and <In|Out|RA>Stream which might freeze the UI thread / throw exception.
			=> Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SetSourceAsync(streamSource.GetInputStreamAt(0).AsStreamForRead()));

		public IAsyncAction SetSourceAsync(IRandomAccessStream streamSource)
			=> AsyncAction.FromTask(ct => SetSourceAsync(streamSource.GetInputStreamAt(0).AsStreamForRead()));
	}
}
