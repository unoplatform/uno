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

			using (var stream = new MemoryStream(Encoding.Default.GetBytes(json)))
			{
				var serializer = new DataContractJsonSerializer(typeof(T));
				return (T)serializer.ReadObject(stream);
			}
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
			using var stream = new MemoryStream();
			var serializer = new DataContractJsonSerializer(typeof(T));
			serializer.WriteObject(stream, value);
			stream.Position = 0;
			using StreamReader reader = new StreamReader(stream);
			return reader.ReadToEnd();
		}
	}
}
