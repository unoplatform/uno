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

			return System.Text.Json.JsonSerializer.Deserialize<T>(json)!;
		}

		public static string Serialize<T>(T value)
		{
			return System.Text.Json.JsonSerializer.Serialize(value);
		}
	}
}
