using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace Uno.Helpers.Serialization
{
	internal static class JsonHelper
	{
		public static T Deserialize<T>(string json)
		{
			if (json is null)
			{
				throw new ArgumentNullException(nameof(json));
			}

			return System.Text.Json.JsonSerializer.Deserialize<T>(json);
		}

		public static bool TryDeserialize<T>(string json, out T value)
		{
			value = default;
			try
			{
				value = Deserialize<T>(json);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		public static string Serialize<T>(T value)
		{
			return System.Text.Json.JsonSerializer.Serialize(value);
		}
	}
}
