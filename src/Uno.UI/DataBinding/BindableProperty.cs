#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// Defines a bindable property implementation.
	/// </summary>
	public class BindableProperty : IBindableProperty
	{
		public BindableProperty(DependencyProperty property)
		{
			DependencyProperty = property;
			PropertyType = property.Type;
		}

		public BindableProperty(Type propertyType, PropertyGetterHandler getter, PropertySetterHandler? setter)
		{
			Getter = getter;
			Setter = setter;
			PropertyType = propertyType;
		}

		public PropertyGetterHandler? Getter { get; }

		public PropertySetterHandler? Setter { get; }

		public Type PropertyType { get; }

		public DependencyProperty? DependencyProperty { get; }
	}
}
