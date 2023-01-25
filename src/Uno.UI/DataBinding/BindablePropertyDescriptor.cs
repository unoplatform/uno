using Uno.UI.DataBinding;
using Windows.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// Defines a bindable property with its owner type
	/// </summary>
	internal class BindablePropertyDescriptor
	{
		private BindablePropertyDescriptor(IBindableType ownerType, IBindableProperty property)
		{
			OwnerType = ownerType;
			Property = property;
		}

		public IBindableType OwnerType { get; }
		public IBindableProperty Property { get; private set; }

		/// <summary>
		/// Gets a bindable type and its property.
		/// </summary>
		/// <param name="originalType">The type in which to look for the property</param>
		/// <param name="property">The property name, or the attached property name.</param>
		/// <returns>A bindable type descriptor.</returns>
		internal static BindablePropertyDescriptor GetPropertByBindableMetadataProvider(Type originalType, string property)
		{
			var bindableType = BindingPropertyHelper.BindableMetadataProvider.GetBindableTypeByType(originalType);

			if (bindableType != null)
			{
				var bindableProperty = bindableType.GetProperty(property);

				if (bindableProperty == null)
				{
					// if the property does not exist, it may be cause it is referencing an attached property.
					// In such a case, the owner type is defined in the property name, and does not relate to 
					// the originalType parameter.

					var dpDescriptor = DependencyPropertyDescriptor.Parse(property);

					if (dpDescriptor != null)
					{
						bindableType = BindingPropertyHelper.BindableMetadataProvider.GetBindableTypeByType(dpDescriptor.OwnerType);

						if (bindableType != null)
						{
							return new BindablePropertyDescriptor(bindableType, bindableType.GetProperty(dpDescriptor.Name));
						}
					}
				}
				else
				{
					return new BindablePropertyDescriptor(bindableType, bindableProperty);
				}
			}

			return new BindablePropertyDescriptor(null, null);
		}

	}
}
