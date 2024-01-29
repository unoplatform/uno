using System;
using Windows.Foundation;
using Windows.Foundation.Metadata;
namespace Microsoft.UI.Xaml.Media.Imaging
{
	[Flags]
	public enum BitmapCreateOptions : uint
	{
		None = 0u,
		IgnoreImageCache = 8u
	}
}
