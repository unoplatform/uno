namespace Windows.UI.Xaml.Media.Imaging
{
#if !(__ANDROID__ || __WASM__)
[global::Uno.NotImplemented]
#endif
	public enum SvgImageSourceLoadStatus
	{
		Success,
		NetworkError,
		InvalidFormat,
		Other,
	}
}
