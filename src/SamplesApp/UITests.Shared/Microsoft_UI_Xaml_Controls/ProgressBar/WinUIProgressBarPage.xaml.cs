using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Uno.UI.Samples.Controls;
using Windows.UI.Xaml.Shapes;

namespace UITests.Microsoft_UI_Xaml_Controls.ProgressBar
{
	[Sample("Progress", "MUX")]
	public sealed partial class WinUIProgressBarPage : Page
	{
		public WinUIProgressBarPage()
		{
			this.InitializeComponent();
			Loaded += ProgressBarPage_Loaded;
		}

		private void ProgressBarPage_Loaded(object sender, RoutedEventArgs e)
		{
			var layoutRoot = (Grid)VisualTreeHelper.GetChild(TestProgressBar, 0);

			VisualStateManager.GetVisualStateGroups(layoutRoot)[0].CurrentStateChanged += this.ProgressBarPage_CurrentStateChanged;
			VisualStateText.Text = VisualStateManager.GetVisualStateGroups(layoutRoot)[0].CurrentState.Name;

			var progressBarRoot = VisualTreeHelper.GetChild(layoutRoot, 0);
			var clip = VisualTreeHelper.GetChild(progressBarRoot, 0);
			var stackPanel = VisualTreeHelper.GetChild(clip, 0);
			var indicator = (Rectangle)VisualTreeHelper.GetChild(stackPanel, 0);

			if (indicator != null)
			{
				indicator.SizeChanged += this.Indicator_SizeChanged;
				IndicatorWidthText.Text = indicator.ActualWidth.ToString();
			}

			Loaded -= ProgressBarPage_Loaded;
		}

		private void Indicator_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			IndicatorWidthText.Text = ((Rectangle)sender).ActualWidth.ToString();
		}

		private void ProgressBarPage_CurrentStateChanged(object sender, VisualStateChangedEventArgs e)
		{
			VisualStateText.Text = e.NewState.Name;
		}

		public void UpdateMinMax_Click(object sender, RoutedEventArgs e)
		{
			TestProgressBar.Maximum = String.IsNullOrEmpty(MaximumInput.Text) ? Double.Parse(MaximumInput.PlaceholderText) : Double.Parse(MaximumInput.Text);
			TestProgressBar.Minimum = String.IsNullOrEmpty(MinimumInput.Text) ? Double.Parse(MinimumInput.PlaceholderText) : Double.Parse(MinimumInput.Text);
		}

		public void UpdateWidth_Click(object sender, RoutedEventArgs e)
		{
			TestProgressBar.Width = String.IsNullOrEmpty(WidthInput.Text) ? Double.Parse(WidthInput.PlaceholderText) : Double.Parse(WidthInput.Text);
		}

		public void UpdateValue_Click(object sender, RoutedEventArgs e)
		{
			TestProgressBar.Value = String.IsNullOrEmpty(ValueInput.Text) ? Double.Parse(ValueInput.PlaceholderText) : Double.Parse(ValueInput.Text);
		}

		public void ChangeValue_Click(object sender, RoutedEventArgs e)
		{
			if (TestProgressBar.Value + 1 > TestProgressBar.Maximum)
			{
				TestProgressBar.Value = (int)(TestProgressBar.Minimum + 0.5);
			}
			else
			{
				TestProgressBar.Value += 1;
			}
		}

		public void UpdatePadding_Click(object sender, RoutedEventArgs e)
		{
			double paddingLeft = String.IsNullOrEmpty(PaddingLeftInput.Text) ? Double.Parse(PaddingLeftInput.PlaceholderText) : Double.Parse(PaddingLeftInput.Text);
			double paddingRight = String.IsNullOrEmpty(PaddingRightInput.Text) ? Double.Parse(PaddingRightInput.PlaceholderText) : Double.Parse(PaddingRightInput.Text);

			TestProgressBar.Padding = new Thickness(paddingLeft, 0, paddingRight, 0);
		}
	}

	public class NullableBooleanToBooleanConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return value is bool ? (bool)value : (object)false;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			return value is bool ? (bool)value : (object)false;
		}
	}
}
