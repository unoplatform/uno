namespace Windows.UI.Xaml.Media
{
	internal interface IImageSource
	{
		void ReportImageLoaded();

		void ReportImageFailed();
	}
}
