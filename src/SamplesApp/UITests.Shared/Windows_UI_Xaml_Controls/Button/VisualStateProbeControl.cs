using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UITests.Shared.Windows_UI_Xaml_Controls.Button
{
	public partial class VisualStateProbeControl : ContentControl
	{

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			ApplyValue();
		}

		public string CurrentState
		{
			get { return (string)GetValue(CurrentStateProperty); }
			set { SetValue(CurrentStateProperty, value); }
		}

		// Using a DependencyProperty as the backing store for CurrentState.  This enables animation, styling, binding, etc...
		public static DependencyProperty CurrentStateProperty { get; } =
			DependencyProperty.Register("CurrentState", typeof(string), typeof(VisualStateProbeControl), new PropertyMetadata("<Unset>", (o, e) => ((VisualStateProbeControl)o).OnCurrentStateChanged((string)e.NewValue)));

		public string ProbeLabel
		{
			get { return (string)GetValue(ProbeLabelProperty); }
			set { SetValue(ProbeLabelProperty, value); }
		}

		// Using a DependencyProperty as the backing store for ProbeLabel.  This enables animation, styling, binding, etc...
		public static DependencyProperty ProbeLabelProperty { get; } =
			DependencyProperty.Register("ProbeLabel", typeof(string), typeof(VisualStateProbeControl), new PropertyMetadata(defaultValue: "", propertyChangedCallback: (o, e) => ((VisualStateProbeControl)o).ApplyValue()));

		private void OnCurrentStateChanged(string newValue)
		{
			ApplyValue();
		}

		private void ApplyValue()
		{
			Content = Content ?? new TextBlock();
			(Content as TextBlock).Text = $"{ProbeLabel}-{CurrentState}";
			WriteLine($"{nameof(VisualStateProbeControl)}-{ProbeLabel} Setting state {CurrentState}");
		}

		private void WriteLine(string line)
		{
#if WINAPPSDK
			Debug.WriteLine(line);
#else
			Console.WriteLine(line);
#endif
		}
	}
}
