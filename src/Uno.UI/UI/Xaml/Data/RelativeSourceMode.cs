namespace Windows.UI.Xaml.Data
{
	/// <summary>
	/// Defines constants that describe the location of the binding source relative to the position of the binding target.
	/// </summary>
	public enum RelativeSourceMode
	{
		/// <summary>
		/// Don't use this value of RelativeSourceMode; always use either Self or TemplatedParent.
		/// </summary>
		None,
		/// <summary>
		/// Refers to the element to which the template (in which the data-bound element exists) is applied. This is similar to setting a TemplateBinding Markup Extension and is only applicable if the Binding is within a template.
		/// </summary>
		TemplatedParent,
		/// <summary>
		/// Refers to the element on which you are setting the binding and allows you to bind one property of that element to another property on the same element.
		/// </summary>
		Self,
	}
}

