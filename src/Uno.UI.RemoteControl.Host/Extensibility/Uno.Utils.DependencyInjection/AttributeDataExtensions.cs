using System;
using System.Linq;
using System.Reflection;

namespace Uno.Utils.DependencyInjection;

internal static class AttributeDataExtensions
{
	/// <summary>
	/// Attempts to create an instance of the specified <typeparamref name="TAttribute"/> type from the provided <see cref="CustomAttributeData"/>.
	/// </summary>
	/// <remarks>This offers the ability to a project to implements their own compatible version of the given <typeparamref name="TAttribute"/> type to reduce dependencies.</remarks>
	/// <param name="data">Data of an attribute.</param>
	/// <returns>An instance of <typeparamref name="TAttribute"/> if the provided <paramref name="data"/> was compatible, `null` otherwise.</returns>
	public static TAttribute? TryCreate<TAttribute>(this CustomAttributeData data)
		=> (TAttribute?)TryCreate(data, typeof(TAttribute));

	/// <summary>
	/// Attempts to create an instance of the specified <paramref name="attribute"/> type from the provided <see cref="CustomAttributeData"/>.
	/// </summary>
	/// <remarks>This offers the ability to a project to implements their own compatible version of the given <paramref name="attribute"/> type to reduce dependencies.</remarks>
	/// <param name="data">Data of an attribute.</param>
	/// <param name="attribute">Type of the attribute to try to instantiate.</param>
	/// <returns>An instance of <paramref name="attribute"/> if the provided <paramref name="data"/> was compatible, `null` otherwise.</returns>
	public static object? TryCreate(this CustomAttributeData data, Type attribute)
	{
		if ((!data.AttributeType.FullName?.Equals(attribute.FullName, StringComparison.Ordinal)) ?? true)
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
			catch { /* Nothing to do, lets try another constructor */ }
		}

		if (instance is null)
		{
			return null; // Failed to find a valid constructor.
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
