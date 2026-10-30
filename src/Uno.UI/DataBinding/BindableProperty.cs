#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// Defines a bindable property implementation.
	/// </summary>
	public class BindableProperty : IBindableProperty
	{
		[UnconditionalSuppressMessage("Trimming", "IL2074", Justification = "Like WinUI, a DependencyProperty's type carries no trimming requirement. A binding that continues through the property's value resolves its next step with the BindableMetadata generated for the app.")]
		public BindableProperty(DependencyProperty property)
		{
			DependencyProperty = property;
			PropertyType = property.Type;
		}


		/// <summary>
		/// This ctor is available for backward compatibility. On newer versions of Uno.UI, the BindableTypeProvidersSourceGenerator uses the single-parameter ctor
		/// </summary>
		public BindableProperty(
			[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
			Type propertyType,
			PropertyGetterHandler getter,
			PropertySetterHandler? setter)
		{
			Getter = getter;
			Setter = setter;
			PropertyType = propertyType;
		}

		public PropertyGetterHandler? Getter { get; }

		public PropertySetterHandler? Setter { get; }

		[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
		public Type PropertyType { get; }

		public DependencyProperty? DependencyProperty { get; }
	}
}
