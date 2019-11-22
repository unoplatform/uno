using System;
using System.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;

namespace Uno.UI.Samples.UITests.Image
{
	[SampleControlInfo("Image")]
	public sealed partial class Image_Stretch_Alignment_Wider : UserControl
	{
		private ArrayList Items = new ArrayList();

		public Image_Stretch_Alignment_Wider()
		{
			this.InitializeComponent();

			var index = 0;

			foreach (var stretch in Enum.GetValues(typeof(Stretch)))
			{
				foreach (var horizontalAlignment in Enum.GetValues(typeof(HorizontalAlignment)))
				{
					foreach (var verticalAlignment in Enum.GetValues(typeof(VerticalAlignment)))
					{
						index++;
						Items.Add(new { index, stretch, horizontalAlignment, verticalAlignment });
					}
				}
			}

		}
	}
}
