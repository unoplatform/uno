using Windows.Foundation;
using Uno.UI.DataBinding;

namespace Windows.UI.Xaml.Controls
{
	public partial class HtmlRadioButtonPresenter : Control
	{
		public HtmlRadioButtonPresenter() : base("input")
		{
			this.RegisterDisposablePropertyChangedCallback(OnPropertyChanged);
			SetAttribute("type", "radio");
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			return MeasureView(availableSize);
		}

		public static readonly DependencyProperty IsCheckedProperty = DependencyProperty.Register(
			"IsChecked", typeof(bool), typeof(HtmlRadioButtonPresenter), new PropertyMetadata(default(bool)));

		public bool IsChecked
		{
			get => (bool)GetValue(IsCheckedProperty);
			set => SetValue(IsCheckedProperty, value);
		}

		private void OnPropertyChanged(ManagedWeakReference instance, DependencyProperty property, DependencyPropertyChangedEventArgs args)
		{
			if (property == IsCheckedProperty)
			{
				SetCheckedState((bool)args.NewValue);
			}
			else if (property == IsEnabledProperty)
			{
				OnEnabilityChanged((bool)args.NewValue);
			}
		}

		private void SetCheckedState(bool isChecked)
		{
			switch (isChecked)
			{
				case true:
					SetProperty("checked", "true");
					return;
				case false:
					SetProperty("checked", "false");
					return;
			}
		}

		private void OnEnabilityChanged(bool isEnabled)
		{
			var disabledTxt = isEnabled ? "false" : "true";
			SetProperty("disabled", disabledTxt);
		}
	}
}
