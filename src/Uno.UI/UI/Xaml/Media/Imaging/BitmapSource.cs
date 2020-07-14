using System;
using System.IO;
using System.Threading.Tasks;

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

			Stream = streamSource;
		}

		public async Task SetSourceAsync(Stream streamSource)
		{

			if (streamSource != null)
			{
				PixelWidth = 0;
				PixelHeight = 0;

				MemoryStream copy = new MemoryStream();
				await streamSource.CopyToAsync(copy);
				Stream = copy;
			}
			else
			{
				//Same behavior as windows, although the documentation does not mention it!!!
				throw new ArgumentException(nameof(streamSource));
			}
		}
	}
}
