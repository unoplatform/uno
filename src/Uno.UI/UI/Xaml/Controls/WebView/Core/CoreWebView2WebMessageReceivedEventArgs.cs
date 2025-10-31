using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Uno.Foundation.Logging;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the WebMessageReceived event.
/// </summary>
public partial class CoreWebView2WebMessageReceivedEventArgs
{
	private static JsonSerializerOptions s_stringSerializerOptions;

	static CoreWebView2WebMessageReceivedEventArgs()
	{
		s_stringSerializerOptions = new JsonSerializerOptions()
		{
			TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
			Converters = {
				JsonMetadataServices.StringConverter,
			},
		};
		s_stringSerializerOptions.MakeReadOnly();
	}

	internal CoreWebView2WebMessageReceivedEventArgs(string webMessageAsJson)
	{
		WebMessageAsJson = webMessageAsJson;
	}

	/// <summary>
	/// Gets the message posted from the WebView content to the host converted to a JSON string.
	/// </summary>
	public string WebMessageAsJson { get; }

	/// <summary>
	/// Gets the message posted from the WebView content to the host as a string.
	/// </summary>
	/// <returns></returns>
	public string TryGetWebMessageAsString()
	{
		if (string.IsNullOrWhiteSpace(WebMessageAsJson))
		{
			return WebMessageAsJson;
		}

		try
		{
			var info = JsonTypeInfo.CreateJsonTypeInfo<string>(s_stringSerializerOptions);

			return JsonSerializer.Deserialize<string>(WebMessageAsJson, info);
		}
		catch (Exception)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("The message could not be deserialized to a string.");
			}

			return null;
		}
	}
}
