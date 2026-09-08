#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Uno.Extensions.Specialized;
using Uno.Helpers.Serialization;
using Uno.Storage.Internal;

namespace Windows.Storage;

internal class DataTypeSerializer
{
	private const char Separator = ':';

	public readonly static Type[] SupportedTypes = new[]
	{
		typeof(bool),
		typeof(byte),
		typeof(char),
		typeof(char),
		typeof(DateTimeOffset),
		typeof(double),
		typeof(Guid),
		typeof(short),
		typeof(int),
		typeof(long),
		typeof(object),
		typeof(Foundation.Point),
		typeof(Foundation.Rect),
		typeof(float),
		typeof(Foundation.Size),
		typeof(string),
		typeof(TimeSpan),
		typeof(byte),
		typeof(ushort),
		typeof(uint),
		typeof(ulong),
		typeof(Uri),
		typeof(ApplicationDataCompositeValue)
	};

	public static string Serialize(object value)
	{
		if (value is null)
		{
			throw new ArgumentNullException(nameof(value));
		}

		var type = value.GetType();

		if (!SupportedTypes.Contains(type))
		{
			throw new NotSupportedException($"Type {value.GetType()} is not supported");
		}

		string serializedValue;
		if (type == typeof(ApplicationDataCompositeValue))
		{
			var composite = (ApplicationDataCompositeValue)value;
			serializedValue = SerializeCompositeValue(composite);
		}
		else
		{
			serializedValue = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
		}

		return value.GetType().FullName + ":" + serializedValue;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "GetType may return null, normal flow of operation")]
	public static object? Deserialize(string? value)
	{
		if (value is null)
		{
			return null;
		}

		var index = value.IndexOf(Separator);

		if (index != -1)
		{
			string typeName = value.Substring(0, index);
			var dataType = Type.GetType(typeName) ?? Type.GetType(typeName + ", " + typeof(Foundation.Point).GetTypeInfo().Assembly.FullName);
			var valueField = value.Substring(index + 1);

			if (dataType == typeof(DateTimeOffset))
			{
				return DateTimeOffset.Parse(valueField, CultureInfo.InvariantCulture);
			}
			else if (dataType == typeof(Guid))
			{
				return Guid.Parse(valueField);
			}
			else if (dataType == typeof(TimeSpan))
			{
				return TimeSpan.Parse(valueField, CultureInfo.InvariantCulture);
			}
			else if (dataType == typeof(ApplicationDataCompositeValue))
			{
				return DeserializeCompositeValue(valueField);
			}
			else if (dataType is not null)
			{
				return Convert.ChangeType(valueField, dataType!, CultureInfo.InvariantCulture);
			}
		}

		return null;
	}

	private static string SerializeCompositeValue(ApplicationDataCompositeValue composite)
	{
		Dictionary<string, string?> targetDictionary = new();
		foreach (var entry in composite)
		{
			string? serializedValue = null;
			if (entry.Value is not null)
			{
				serializedValue = Serialize(entry.Value);
			}

			targetDictionary.Add(entry.Key, serializedValue);
		}

		return JsonHelper.Serialize(targetDictionary, DataTypeSerializerContext.Default);
	}

	private static ApplicationDataCompositeValue DeserializeCompositeValue(string value)
	{
		var dictionary = JsonHelper.Deserialize<Dictionary<string, string?>>(value, DataTypeSerializerContext.Default);
		if (dictionary is null)
		{
			throw new InvalidOperationException("Failed to deserialize ApplicationDataCompositeValue");
		}

		var composite = new ApplicationDataCompositeValue();
		foreach (var entry in dictionary)
		{
			if (Deserialize(entry.Value) is { } nonNullValue)
			{
				composite.Add(entry.Key, nonNullValue);
			}
		}

		return composite;
	}
}
