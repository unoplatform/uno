#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Extensions;
using System.Collections;
using Microsoft.UI.Xaml;

namespace Uno.UI.DataBinding
{
	/// <summary>
	/// Defines a bindable type implementation
	/// </summary>
	public class BindableType : IBindableType
	{
		internal const DynamicallyAccessedMemberTypes TypeRequirements =
			  DynamicallyAccessedMemberTypes.NonPublicConstructors  // DependencyProperty.Register()
			| DynamicallyAccessedMemberTypes.PublicConstructors
			| DynamicallyAccessedMemberTypes.PublicFields
			| DynamicallyAccessedMemberTypes.PublicProperties
			;

		private readonly Hashtable _properties;
		private StringIndexerGetterDelegate? _stringIndexerGetter;
		private StringIndexerSetterDelegate? _stringIndexerSetter;
		private ActivatorDelegate? _activator;

		/// <summary>
		/// Builds a new BindableType.
		/// </summary>
		/// <param name="estimatedPropertySize">Provide an estimated number of properties, so the dictionary does not need to grow unnecessarily.</param>
		/// <param name="sourceType">The actual .NET type that corresponds to this instance.</param>
		public BindableType(
			int estimatedPropertySize,
			[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
			Type sourceType)
		{
			_properties = new Hashtable(estimatedPropertySize);
			Type = sourceType;
		}

		[DynamicallyAccessedMembers(BindableType.TypeRequirements)]
		public Type Type { get; }

		public ActivatorDelegate? CreateInstance()
		{
			return _activator;
		}

		public IBindableProperty? GetProperty(string name)
		{
			var property = _properties[name] as IBindableProperty;

			if (property == null)
			{
				var prop = DependencyPropertyDescriptor.Parse(name);

				if (prop != null && prop.OwnerType.IsAssignableFrom(Type))
				{
					property = GetProperty(prop.Name);
				}
			}

			return property;
		}

		public void AddActivator(ActivatorDelegate activator)
		{
			_activator = activator;
		}

		public void AddProperty<
			[DynamicallyAccessedMembers(TypeRequirements)] T
		>(string name, PropertyGetterHandler getter, PropertySetterHandler? setter = null)
		{
			_properties[name] = new BindableProperty(typeof(T), getter, setter);
		}

		public void AddProperty(DependencyProperty property)
		{
			_properties[property.Name] = new BindableProperty(property);
		}

		public void AddProperty(
			string name,
			[DynamicallyAccessedMembers(TypeRequirements)]
			Type propertyType,
			PropertyGetterHandler getter,
			PropertySetterHandler? setter = null)
		{
			_properties[name] = new BindableProperty(propertyType, getter, setter);
		}

		public void AddIndexer(StringIndexerGetterDelegate getter, StringIndexerSetterDelegate setter)
		{
			_stringIndexerGetter = getter;
			_stringIndexerSetter = setter;
		}

		public StringIndexerGetterDelegate? GetIndexerGetter()
		{
			return _stringIndexerGetter;
		}

		public StringIndexerSetterDelegate? GetIndexerSetter()
		{
			return _stringIndexerSetter;
		}
	}
}
