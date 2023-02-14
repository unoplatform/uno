namespace Microsoft.UI.Xaml.Data
{
	/// <summary>
	/// Describes how the data propagates in a binding.
	/// </summary>
	public enum BindingMode
	{
		/// <summary>
		/// Updates the target property when the binding is created. Changes to the source object can also propagate to the target.
		/// </summary>
		OneWay,
		/// <summary>
		/// Updates the target property when the binding is created.
		/// </summary>
		OneTime,
		/// <summary>
		/// Updates either the target or the source object when either changes. When the binding is created, the target property is updated from the source.
		/// </summary>
		TwoWay,
	}
}

