using System;
using System.Linq;
using System.Reflection;

namespace Uno.Extensions.DependencyInjection;

internal static class AttributeDataExtensions
{
	public static TAttribute? TryCreate<TAttribute>(this CustomAttributeData data)
		=> (TAttribute?)TryCreate(data, typeof(TAttribute));

	public static object? TryCreate(this CustomAttributeData data, Type attribute)
	{
		if (!data.AttributeType.FullName?.Equals(attribute.FullName, StringComparison.Ordinal) ?? false)
		{
			return null;
		}

		var instance = default(object);
		foreach (var ctor in attribute.GetConstructors())
		{
			var parameters = ctor.GetParameters();
			var arguments = data.ConstructorArguments;
			if (arguments.Count > parameters.Length
				|| arguments.Count < parameters.Count(p => !p.IsOptional))
			{
				continue;
			}

			var argumentsCompatible = true;
			var args = new object?[parameters.Length];
			for (var i = 0; argumentsCompatible && i < arguments.Count; i++)
			{
				argumentsCompatible &= parameters[i].ParameterType == arguments[i].ArgumentType;
				args[i] = arguments[i].Value;
			}

			if (!argumentsCompatible)
			{
				continue;
			}

			try
			{
				instance = ctor.Invoke(args);
				break;
			}
			catch { }
		}

		if (instance is null)
		{
			return null;
		}

		try
		{
			var properties = attribute
				.GetProperties()
				.Where(prop => prop.CanWrite)
				.ToDictionary(prop => prop.Name, StringComparer.Ordinal);
			var fields = attribute
				.GetFields()
				.Where(field => !field.IsInitOnly)
				.ToDictionary(field => field.Name, StringComparer.Ordinal);
			foreach (var member in data.NamedArguments)
			{
				if (member.IsField)
				{
					if (fields.TryGetValue(member.MemberName, out var field)
						&& field.FieldType.IsAssignableFrom(member.TypedValue.ArgumentType))
					{
						field.SetValue(instance, member.TypedValue.Value);
					}
					else
					{
						return null;
					}
				}
				else
				{
					if (properties.TryGetValue(member.MemberName, out var prop)
						&& prop.PropertyType.IsAssignableFrom(member.TypedValue.ArgumentType))
					{
						prop.SetValue(instance, member.TypedValue.Value);
					}
					else
					{
						return null;
					}
				}
			}

			return instance;
		}
		catch
		{
			return null;
		}
	}
}
