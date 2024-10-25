namespace Windows.UI.Xaml
{
	public partial class StateTrigger : StateTriggerBase
	{
		public bool IsActive
		{
			get => (bool)GetValue(IsActiveProperty);
			set => SetValue(IsActiveProperty, value);
		}

		public static DependencyProperty IsActiveProperty { get; } =
			Windows.UI.Xaml.DependencyProperty.Register(
				"IsActive", typeof(bool),
				typeof(StateTrigger),
				new FrameworkPropertyMetadata(
					defaultValue: default(bool),
					propertyChangedCallback: (s, e) => (s as StateTrigger)?.OnIsActiveChanged(e)));

		private void OnIsActiveChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is bool b)
			{
				SetActive(b);
			}
		}
	}
}
