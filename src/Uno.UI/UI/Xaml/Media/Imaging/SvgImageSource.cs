#nullable enable
using System;
using System.ComponentModel;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI.Core;
using Uno;

namespace Windows.UI.Xaml.Media.Imaging
{
	public partial class SvgImageSource : ImageSource
	{
		public Uri UriSource
		{
			get => (Uri)GetValue(UriSourceProperty);
			set => SetValue(UriSourceProperty, value);
		}

		public static DependencyProperty UriSourceProperty { get; } =
			DependencyProperty.Register(
				nameof(UriSource), typeof(Uri),
				typeof(SvgImageSource),
				new FrameworkPropertyMetadata(default(Uri), (s, e) => (s as SvgImageSource)?.OnUriSourceChanged(e)));

		private void OnUriSourceChanged(DependencyPropertyChangedEventArgs e)
		{
			if (!object.Equals(e.OldValue, e.NewValue))
			{
				UnloadImageData();
			}
			InitFromUri(e.NewValue as Uri);
#if NETSTANDARD
			InvalidateSource();
#endif
		}

		public SvgImageSource()
		{
			InitPartial();
		}

		public SvgImageSource(Uri uriSource)
		{
			UriSource = uriSource;

			InitPartial();
		}

		public async Task SetSourceAsync(Stream streamSource)
		{
			if (streamSource == null)
			{
				//Same behavior as windows, although the documentation does not mention it!!!
				throw new ArgumentException(nameof(streamSource));
			}

			var copy = new MemoryStream();
			await streamSource.CopyToAsync(copy);
			copy.Position = 0;
			Stream = copy;

#if NETSTANDARD
			InvalidateSource();
#endif
		}

		public void SetSource(IRandomAccessStream streamSource)
			// We prefer to use the SetSourceAsync here in order to make sure that the stream is copied ASYNChronously,
			// which is important since we are using a stream wrapper of and <In|Out|RA>Stream which might freeze the UI thread / throw exception.
			=> Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => SetSourceAsync(streamSource.GetInputStreamAt(0).AsStreamForRead()));

		public IAsyncAction SetSourceAsync(IRandomAccessStream streamSource)
			=> AsyncAction.FromTask(ct => SetSourceAsync(streamSource.GetInputStreamAt(0).AsStreamForRead()));

		partial void InitPartial();

#pragma warning disable 67
		public event Foundation.TypedEventHandler<SvgImageSource, SvgImageSourceFailedEventArgs>? OpenFailed;

		public event Foundation.TypedEventHandler<SvgImageSource, SvgImageSourceOpenedEventArgs>? Opened;
#pragma warning restore 67
	}
}
