using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.UI.Xaml.Controls;

partial class Image
{
	/// <summary>
	/// Returns an Empty string as the Description for the Image.
	/// </summary>
	internal
#if __MACOS__ || __IOS__
		new
#endif
		string Description => _imageSource?.Description;
}
