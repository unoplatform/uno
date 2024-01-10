using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

namespace UITests.Windows_UI_Xaml_Controls.Repeater
{
	public sealed partial class MyItem : Page
	{
		private static int _count;

		private int InstanceId { get; } = _count++;

		public MyItem()
		{
			this.InitializeComponent();
		}

		private Brush GetColor(object value)
		{
			if (int.TryParse(value?.ToString(), out var id))
			{
				switch (id % 6)
				{
					case 0: return new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x00, 0x00));
					case 1: return new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0x7F, 0x00));
					case 2: return new SolidColorBrush(Color.FromArgb(0xFF, 0xFF, 0xFF, 0x00));
					case 3: return new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0xFF, 0x00));
					case 4: return new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x00, 0xFF));
					case 5: return new SolidColorBrush(Color.FromArgb(0xFF, 0x94, 0x00, 0xD3));
				}
			}

			return new SolidColorBrush(Colors.Black);
		}
	}
}
