using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// Defines a bindable property implementation.
	/// </summary>
	public class BindableProperty : IBindableProperty
	{
		public BindableProperty(Type propertyType, PropertyGetterHandler getter, PropertySetterHandler setter)
		{
			Getter = getter;
			Setter = setter;
			PropertyType = propertyType;
		}

		public PropertyGetterHandler Getter { get; }

		public PropertySetterHandler Setter { get; }

		public Type PropertyType { get; }
	}
}
