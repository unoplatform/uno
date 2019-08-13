using System;
using System.Collections.Generic;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.LayoutTestControl
{
	public partial class MeasureArrangeContentControl : ContentControl
	{
		private MeasureArrangeCounter _counterControl;

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			var updateButton = this.GetTemplateChild("PART_UpdateButton") as Windows.UI.Xaml.Controls.Button;
			_counterControl = this.GetTemplateChild("PART_CounterControl") as MeasureArrangeCounter;

			if (updateButton != null)
			{
				updateButton.Click += OnUpdateButtonClicked;
			}
			else
			{
				throw new InvalidOperationException("PART_UpdateButton is missing in MeasureArrangeCounter");
			}

			if (_counterControl == null)
			{
				throw new InvalidOperationException("PART_CounterControl is missing in MeasureArrangeCounter");
			}
		}

		private void OnUpdateButtonClicked(object sender, RoutedEventArgs e)
		{
			_counterControl.UpdateCounts();
		}
	}
}
