namespace Windows.UI.Xaml.Data
{
	/// <summary>
	/// Defines constants that indicate when a binding source is updated by its binding target in two-way binding.
	/// </summary>
	public enum UpdateSourceTrigger
	{
		/// <summary>
		/// Use default behavior from the dependency property that uses the binding. In Windows Runtime, this evaluates the same as a value with PropertyChanged.
		/// </summary>
		Default = 0,
		/// <summary>
		/// The binding source is updated whenever the binding target value changes. This is detected automatically by the binding system.
		/// </summary>
		PropertyChanged,
		/// <summary>
		/// The binding source is updated only when you call the BindingExpression.UpdateSource method.
		/// </summary>
		Explicit,
	}
}

