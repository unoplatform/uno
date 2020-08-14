using System;
using System.Linq;

namespace Windows.UI.Xaml.Media
{
	internal enum ImageDataKind
	{
		/// <summary>
		/// The source is empty and the target visual element should be cleared
		/// </summary>
		Empty,

		Url,

		/// <summary>
		/// A base64 encoded image
		/// </summary>
		Base64,

		/// <summary>
		/// The image failed to load (cf. The Error property)
		/// </summary>
		Error = 256
	}
}
