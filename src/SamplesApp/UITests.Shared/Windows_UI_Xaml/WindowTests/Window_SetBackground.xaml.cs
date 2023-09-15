using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Uno.UI.Samples.Controls;
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

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UITests.Windows_UI_Xaml.WindowTests
{
	[Sample("Windowing")]
	public sealed partial class Window_SetBackground : UserControl
	{
		private Color _selectedColor;

		public Window_SetBackground()
		{
			this.InitializeComponent();

#if HAS_UNO
			_selectedColor = (Window.CurrentSafe.Background as SolidColorBrush)?.Color ?? Colors.White;
#endif
		}

		public Color SelectedColor
		{
			get => _selectedColor;
			set
			{
				_selectedColor = value;

#if HAS_UNO
				if (Window.CurrentSafe is { }) // could be null if on WinUI tree
				{
					Window.CurrentSafe.Background = new SolidColorBrush(_selectedColor);
				}
#endif
			}
		}
	}
}
