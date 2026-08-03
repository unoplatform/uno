#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Windows.AppNotifications.Internal;

internal static class AppNotificationArgumentCodec
{
	public static string Encode(IEnumerable<KeyValuePair<string, string>> arguments)
	{
		var encodedArguments = new List<KeyValuePair<string, string>>();
		foreach (var argument in arguments)
		{
			encodedArguments.Add(new KeyValuePair<string, string>(EncodeComponent(argument.Key), EncodeComponent(argument.Value)));
		}

		return SerializeEncoded(encodedArguments);
	}

	public static string SerializeEncoded(IEnumerable<KeyValuePair<string, string>> arguments)
	{
		var encoded = new StringBuilder();

		foreach (var argument in arguments)
		{
			if (encoded.Length > 0)
			{
				encoded.Append(';');
			}

			encoded.Append(argument.Key);
			if (argument.Value.Length > 0)
			{
				encoded.Append('=');
				encoded.Append(argument.Value);
			}
		}

		return encoded.ToString();
	}

	public static string EncodeComponent(string value)
	{
		var encoded = new StringBuilder(value.Length);
		AppendEncoded(encoded, value);
		return encoded.ToString();
	}

	public static IDictionary<string, string> Decode(string encodedArguments)
	{
		var arguments = new Dictionary<string, string>();
		if (encodedArguments.Length == 0)
		{
			return arguments;
		}

		foreach (var encodedArgument in encodedArguments.Split(';'))
		{
			var separatorIndex = encodedArgument.IndexOf('=');
			if (separatorIndex < 0)
			{
				arguments[DecodeComponent(encodedArgument)] = string.Empty;
				continue;
			}

			var key = DecodeComponent(encodedArgument[..separatorIndex]);
			var value = DecodeComponent(encodedArgument[(separatorIndex + 1)..]);
			arguments[key] = value;
		}

		return arguments;
	}

	private static void AppendEncoded(StringBuilder encoded, string value)
	{
		foreach (var character in value)
		{
			encoded.Append(character switch
			{
				'%' => "%25",
				';' => "%3B",
				'=' => "%3D",
				'&' => "&amp;",
				'\"' => "&quot;",
				'\'' => "&apos;",
				'<' => "&lt;",
				'>' => "&gt;",
				_ => character.ToString()
			});
		}
	}

	private static string DecodeComponent(string value)
	{
		var decoded = new StringBuilder(value.Length);

		for (var index = 0; index < value.Length; index++)
		{
			if (value[index] == '%' && index + 2 < value.Length)
			{
				var escape = value.AsSpan(index, 3);
				if (escape.SequenceEqual("%25"))
				{
					decoded.Append('%');
					index += 2;
					continue;
				}

				if (escape.SequenceEqual("%3B"))
				{
					decoded.Append(';');
					index += 2;
					continue;
				}

				if (escape.SequenceEqual("%3D"))
				{
					decoded.Append('=');
					index += 2;
					continue;
				}
			}

			decoded.Append(value[index]);
		}

		return decoded.ToString();
	}
}
