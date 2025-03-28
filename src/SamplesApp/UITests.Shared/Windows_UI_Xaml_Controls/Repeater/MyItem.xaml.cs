using System;
using System.Linq;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UITests.Windows_UI_Xaml_Controls.Repeater
{
	public sealed partial class MyItem : Page
	{
		private static int _count;

		private int InstanceId { get; } = _count++;

		private int InstanceCount => _count;

		public MyItem()
		{
			this.InitializeComponent();
		}

		private Brush GetColor(object value)
		{
			if (int.TryParse(value?.ToString(), out var id))
			{
				return (id % 6) switch
				{
					0 => new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00)),
					1 => new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x7F, 0x00)),
					2 => new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0x00)),
					3 => new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xFF, 0x00)),
					4 => new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xFF)),
					_ => new SolidColorBrush(Color.FromArgb(0xFF, 0x94, 0x00, 0xD3))
				};
			}

			return new SolidColorBrush(Colors.Black);
		}
	}
}
