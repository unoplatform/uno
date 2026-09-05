using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace Uno.Helpers.Serialization
{
	internal static class JsonHelper
	{
		public static T Deserialize<T>(string json, JsonSerializerContext context)
		{
			ArgumentNullException.ThrowIfNull(json);

			return (T)System.Text.Json.JsonSerializer.Deserialize(json, typeof(T), context);
		}

		public static bool TryDeserialize<T>(string json, out T value, JsonSerializerContext context)
		{
			value = default;
			try
			{
				value = Deserialize<T>(json, context);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static string Serialize<T>(T value, JsonSerializerContext context)
			=> System.Text.Json.JsonSerializer.Serialize(value, typeof(T), context);
	}
}
