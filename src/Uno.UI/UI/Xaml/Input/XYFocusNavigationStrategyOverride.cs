namespace Windows.UI.Xaml.Input
{
	/// <summary>
	/// Specifies how the XAML framework determines the target of an XY navigation.
	/// </summary>
	public enum XYFocusNavigationStrategyOverride
	{
		/// <summary>
		/// No navigation override is applied.
		/// </summary>
		None,

		/// <summary>
		/// Indicates that navigation strategy is inherited from the element's ancestors.
		/// If all ancestors have a value of Auto, the fallback strategy is Projection.
		/// </summary>
		Auto,

		/// <summary>
		/// Indicates that focus moves to the first element encountered when projecting
		/// the edge of the currently focused element in the direction of navigation.
		/// </summary>
		Projection,

		/// <summary>
		/// Indicates that focus moves to the element closest to the axis of the navigation direction.
		/// </summary>
		NavigationDirectionDistance,

		/// <summary>
		/// Indicates that focus moves to the closest element based on the shortest 2D distance (Manhattan metric).
		/// </summary>
		RectilinearDistance,
	}
}
