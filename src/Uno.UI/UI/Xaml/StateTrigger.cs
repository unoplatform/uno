namespace Microsoft.UI.Xaml
{
	public partial class StateTrigger : StateTriggerBase
	{
#if UNO_HAS_ENHANCED_LIFECYCLE
		public new bool IsActive
#else
		public bool IsActive
#endif
		{
			get => (bool)GetValue(IsActiveProperty);
			set => SetValue(IsActiveProperty, value);
		}

		public static DependencyProperty IsActiveProperty { get; } =
			Microsoft.UI.Xaml.DependencyProperty.Register(
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
