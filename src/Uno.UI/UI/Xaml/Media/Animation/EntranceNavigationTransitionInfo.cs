namespace Windows.UI.Xaml.Media.Animation
{
	public partial class EntranceNavigationTransitionInfo : NavigationTransitionInfo
	{
		public EntranceNavigationTransitionInfo() : base() { }

		#region IsTargetElement

		public static bool GetIsTargetElement(UIElement element)
		{
			return (bool)element.GetValue(IsTargetElementProperty);
		}

		public static void SetIsTargetElement(UIElement element, bool value)
		{
			element.SetValue(IsTargetElementProperty, value);
		}

		public static DependencyProperty IsTargetElementProperty { get; } =
			DependencyProperty.RegisterAttached(
			"IsTargetElement", typeof(bool),
			typeof(EntranceNavigationTransitionInfo),
			new FrameworkPropertyMetadata(default(bool))
		);

		#endregion
	}
}
