using System;
using System.Collections.Generic;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UITests.Windows_UI_Xaml_Controls.ImageTests
{
	[Sample("Image")]
	public sealed partial class Image_Formats : Page
	{
		internal static Dictionary<string, string> Formats = new Dictionary<string, string>()
		{
			{"bmp (bitmap)", "ms-appx:///Assets/Formats/uno-overalls.bmp"},
			{"gif (bitmap)", "ms-appx:///Assets/Formats/uno-overalls.gif"},
			{"heic (bitmap)", "ms-appx:///Assets/Formats/uno-overalls.heic"},
			{"jpg (bitmap)", "ms-appx:///Assets/Formats/uno-overalls.jpg"},
			{"png (bitmap)", "ms-appx:///Assets/Formats/uno-overalls.png"},
			{"pdf (vector)", "ms-appx:///Assets/Formats/uno-overalls.pdf"},
			{"svg (vector)", "ms-appx:///Assets/Formats/uno-overalls.svg"},
			{"webp (bitmap)", "ms-appx:///Assets/Formats/uno-overalls.webp"}
		};

		private Dictionary<string, string> Images => Formats;

		public Image_Formats()
		{
			this.InitializeComponent();
		}
	}
}
