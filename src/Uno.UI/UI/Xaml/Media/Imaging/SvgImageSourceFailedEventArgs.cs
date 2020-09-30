namespace Windows.UI.Xaml.Media.Imaging
{
	public partial class SvgImageSourceFailedEventArgs
	{
#if __ANDROID__ || __IOS__
		[global::Uno.NotImplemented("__ANDROID__", "__IOS__", "NET461", "__SKIA__", "__NETSTD_REFERENCE__", "__MACOS__")]
#endif
		public SvgImageSourceLoadStatus Status { get; }

		internal SvgImageSourceFailedEventArgs(SvgImageSourceLoadStatus status)
		{
			Status = status;
		}
	}
}
