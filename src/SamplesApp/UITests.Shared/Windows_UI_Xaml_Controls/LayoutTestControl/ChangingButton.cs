using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.LayoutTestControl
{
	public enum ChangingMode
	{
		All,
		WidthOnly,
		HeightOnly,
	}

	public partial class ChangingButton : Windows.UI.Xaml.Controls.Button
	{
		public ChangingMode ChangingMode
		{
			get { return (ChangingMode)GetValue(ChangingModeProperty); }
			set { SetValue(ChangingModeProperty, value); }
		}

		public static readonly DependencyProperty ChangingModeProperty =
			DependencyProperty.Register("ChangingMode", typeof(ChangingMode), typeof(ChangingButton), new PropertyMetadata(ChangingMode.All));

		public ChangingButton()
		{
			this.Click += OnClicked;
		}

		private double[] _sizes = new double[] { 100, 150, 300, 500 };
		private int _currentIndex = 0;

		private void OnClicked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
		{
			if (ChangingMode == ChangingMode.All || ChangingMode == ChangingMode.WidthOnly)
			{
				this.Width = _sizes[_currentIndex % (_sizes.Length - 1)];
			}

			if (ChangingMode == ChangingMode.All || ChangingMode == ChangingMode.HeightOnly)
			{
				this.Height = _sizes[_currentIndex % (_sizes.Length - 1)];
			}

			_currentIndex++;
		}
	}
}
