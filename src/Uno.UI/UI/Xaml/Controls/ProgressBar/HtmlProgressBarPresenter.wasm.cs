using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Controls
{
	public partial class HtmlProgressBarPresenter : Control
	{
		public HtmlProgressBarPresenter() : base("progress")
		{
			this.RegisterDisposablePropertyChangedCallback(OnPropertyChanged);
			SetAttribute("max", "1");
		}

		public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
			"Maximum", typeof(double), typeof(HtmlProgressBarPresenter), new PropertyMetadata(default(double)));

		public double Maximum
		{
			get => (double)GetValue(MaximumProperty);
			set => SetValue(MaximumProperty, value);
		}

		public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
			"Value", typeof(double), typeof(HtmlProgressBarPresenter), new PropertyMetadata(default(double)));

		public double Value
		{
			get => (double)GetValue(ValueProperty);
			set => SetValue(ValueProperty, value);
		}

		private void OnPropertyChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
			if (property == ValueProperty || property == MaximumProperty)
			{
				SetValue();
			}
		}

		private void SetValue()
		{
			var max = Maximum;
			if (max <= 0)
			{
				max = 1;
			}

			var ratioValue = Value / max;
			SetAttribute("value", ratioValue.ToStringInvariant());
		}
	}
}
