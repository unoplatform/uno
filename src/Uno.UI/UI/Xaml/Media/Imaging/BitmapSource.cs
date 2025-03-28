using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Uno.UI.Xaml.Media;
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
		public static DependencyProperty PixelHeightProperty { get; } =
			DependencyProperty.Register("PixelHeight", typeof(int), typeof(BitmapSource), new FrameworkPropertyMetadata(0));

		#endregion

		#region PixelWidth DependencyProperty

		public int PixelWidth
		{
			get { return (int)GetValue(PixelWidthProperty); }
			internal set { SetValue(PixelWidthProperty, value); }
		}

		// Using a DependencyProperty as the backing store for PixelWidth.  This enables animation, styling, binding, etc...
		public static DependencyProperty PixelWidthProperty { get; } =
			DependencyProperty.Register("PixelWidth", typeof(int), typeof(BitmapSource), new FrameworkPropertyMetadata(0));

		#endregion

#if __CROSSRUNTIME__ || IS_UNIT_TESTS
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

			// The source has to be cloned before leaving the "SetSource[Async]".
			var clonedStreamSource = streamSource.CloneStream();

#if __CROSSRUNTIME__ || IS_UNIT_TESTS
			_stream = clonedStreamSource;
			UpdatePixelWidthAndHeightPartial(_stream.CloneStream().AsStream());
#else
			Stream = clonedStreamSource.AsStream();
			UpdatePixelWidthAndHeightPartial(clonedStreamSource.AsStream());
#endif
			OnSetSource();
		}

		partial void UpdatePixelWidthAndHeightPartial(Stream stream);
		private protected virtual void OnSetSource() { }

		private
#if __CROSSRUNTIME__
			async
#endif
			Task ForceLoad(CancellationToken ct)
		{
#if __CROSSRUNTIME__
			var tcs = new TaskCompletionSource<object>();
			using var r = ct.Register(() => tcs.TrySetCanceled());
			using var s = Subscribe(OnChanged);

			InvalidateSource();

			await tcs.Task;

			void OnChanged(ImageData data)
			{
				tcs.TrySetResult(null);
			}
#else
			StreamLoaded?.Invoke(this, EventArgs.Empty);
			return Task.CompletedTask;
#endif
		}

#if !__CROSSRUNTIME__
		internal event EventHandler StreamLoaded;
#endif

		public override string ToString()
		{
			if (AbsoluteUri is { } uri)
			{
				return $"{GetType().Name}/{uri}";
			}

#if __CROSSRUNTIME__ || IS_UNIT_TESTS
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
