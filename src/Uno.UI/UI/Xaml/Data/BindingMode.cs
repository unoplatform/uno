namespace Windows.UI.Xaml.Data
{
	/// <summary>
	/// Describes how the data propagates in a binding.
	/// </summary>
	public enum BindingMode
	{
		/// <summary>
		/// Updates the target property when the binding is created. Changes to the source object can also propagate to the target.
		/// </summary>
		OneWay = 1,
		/// <summary>
		/// Updates the target property when the binding is created.
		/// </summary>
		OneTime = 2,
		/// <summary>
		/// Updates either the target or the source object when either changes. When the binding is created, the target property is updated from the source.
		/// </summary>
		TwoWay = 3,
	}
}

