using System;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Metadata;

namespace Windows.UI.Xaml.Media.Imaging
{
	public sealed partial class BitmapImage : BitmapSource
	{
		public event DownloadProgressEventHandler DownloadProgress;

		public event ExceptionRoutedEventHandler ImageFailed;

		public event RoutedEventHandler ImageOpened;

		#region UriSource DependencyProperty

		public Uri UriSource
		{
			get { return (Uri)GetValue(UriSourceProperty); }
			set { SetValue(UriSourceProperty, value); }
		}

		// Using a DependencyProperty as the backing store for UriSource.  This enables animation, styling, binding, etc...
		public static DependencyProperty UriSourceProperty { get ; } =
			DependencyProperty.Register("UriSource", typeof(Uri), typeof(BitmapImage), new FrameworkPropertyMetadata(null, (s, e) => ((BitmapImage)s)?.OnUriSourceChanged(e)));

		private void OnUriSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			if (!object.Equals(e.OldValue, e.NewValue))
			{
				UnloadImageData();
			}
			InitFromUri(e.NewValue as Uri);
#if __WASM__
			InvalidateSource();
#endif
		}

		#endregion

		#region DecodePixelType DependencyProperty

		public DecodePixelType DecodePixelType
		{
			get { return (DecodePixelType)GetValue(DecodePixelTypeProperty); }
			set { SetValue(DecodePixelTypeProperty, value); }
		}

		// Using a DependencyProperty as the backing store for DecodePixelType.  This enables animation, styling, binding, etc...
		public static DependencyProperty DecodePixelTypeProperty { get ; } =
			DependencyProperty.Register("DecodePixelType", typeof(DecodePixelType), typeof(BitmapImage), new FrameworkPropertyMetadata(DecodePixelType.Physical, (s, e) => ((BitmapImage)s)?.OnDecodePixelTypeChanged(e)));


		private void OnDecodePixelTypeChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region DecodePixelWidth DependencyProperty

		public int DecodePixelWidth
		{
			get { return (int)GetValue(DecodePixelWidthProperty); }
			set { SetValue(DecodePixelWidthProperty, value); }
		}

		// Using a DependencyProperty as the backing store for DecodePixelWidth.  This enables animation, styling, binding, etc...
		public static DependencyProperty DecodePixelWidthProperty { get ; } =
			DependencyProperty.Register("DecodePixelWidth", typeof(int), typeof(BitmapImage), new FrameworkPropertyMetadata(0, (s, e) => ((BitmapImage)s)?.OnDecodePixelWidthChanged(e)));


		private void OnDecodePixelWidthChanged(DependencyPropertyChangedEventArgs e)
		{

		}

		#endregion

		#region DecodePixelHeight DependencyProperty

		public int DecodePixelHeight
		{
			get { return (int)GetValue(DecodePixelHeightProperty); }
			set { SetValue(DecodePixelHeightProperty, value); }
		}

		// Using a DependencyProperty as the backing store for DecodePixelHeight.  This enables animation, styling, binding, etc...
		public static DependencyProperty DecodePixelHeightProperty { get ; } =
			DependencyProperty.Register("DecodePixelHeight", typeof(int), typeof(BitmapImage), new FrameworkPropertyMetadata(0, (s, e) => ((BitmapImage)s)?.OnDecodePixelHeightChanged(e)));


		private void OnDecodePixelHeightChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		#region CreateOptions DependencyProperty

		public BitmapCreateOptions CreateOptions
		{
			get { return (BitmapCreateOptions)GetValue(CreateOptionsProperty); }
			set { SetValue(CreateOptionsProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CreateOptions.  This enables animation, styling, binding, etc...
		public static DependencyProperty CreateOptionsProperty { get ; } =
			DependencyProperty.Register("CreateOptions", typeof(BitmapCreateOptions), typeof(BitmapImage), new FrameworkPropertyMetadata(BitmapCreateOptions.None, (s, e) => ((BitmapImage)s)?.OnCreateOptionsChanged(e)));


		private void OnCreateOptionsChanged(DependencyPropertyChangedEventArgs e)
		{
		}

		#endregion

		public BitmapImage(Uri uriSource) : base(uriSource)
		{
			UriSource = uriSource;
		}

		internal BitmapImage(string stringSource) : base(stringSource)
		{
			UriSource = WebUri;
		}

		public BitmapImage() { }

		private void RaiseDownloadProgress(DownloadProgressEventArgs args)
		{
			DownloadProgress?.Invoke(this, args);
		}

		private void RaiseImageFailed(ExceptionRoutedEventArgs args)
		{
			ImageFailed?.Invoke(this, args);
		}

		private void RaiseImageOpened(RoutedEventArgs args)
		{
			ImageOpened?.Invoke(this, args);
		}
	}
}
