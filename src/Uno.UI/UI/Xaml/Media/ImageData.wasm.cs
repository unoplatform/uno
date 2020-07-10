using System;
using System.Linq;

namespace Windows.UI.Xaml.Media
{
	/// <summary>
	/// Represents the raw data of an **opened** image source
	/// </summary>
	internal struct ImageData
	{
		public ImageDataKind Kind { get; set; }

		public string Value { get; set; }

		public Exception Error { get; set; }
	}
}
