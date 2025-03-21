namespace Windows.UI.Xaml.Media.Animation
{
	public partial class CommonNavigationTransitionInfo : NavigationTransitionInfo
	{
		public CommonNavigationTransitionInfo() : base() { }

		#region IsStaggeringEnabled

		public bool IsStaggeringEnabled
		{
			get => (bool)this.GetValue(IsStaggeringEnabledProperty);
			set => this.SetValue(IsStaggeringEnabledProperty, value);
		}

		public static DependencyProperty IsStaggeringEnabledProperty { get; } =
		DependencyProperty.Register(
			"IsStaggeringEnabled", typeof(bool),
			typeof(CommonNavigationTransitionInfo),
			new FrameworkPropertyMetadata(default(bool))
		);

		#endregion

		#region IsStaggerElement

		public static bool GetIsStaggerElement(UIElement element)
		{
			return (bool)element.GetValue(IsStaggerElementProperty);
		}

		public static void SetIsStaggerElement(UIElement element, bool value)
		{
			element.SetValue(IsStaggerElementProperty, value);
		}

		public static DependencyProperty IsStaggerElementProperty { get; } =
		DependencyProperty.RegisterAttached(
			"IsStaggerElement", typeof(bool),
			typeof(CommonNavigationTransitionInfo),
			new FrameworkPropertyMetadata(default(bool))
		);

		#endregion
	}
}
