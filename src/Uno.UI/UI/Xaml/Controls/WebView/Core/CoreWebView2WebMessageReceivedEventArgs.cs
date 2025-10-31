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
			var serializerOptions = new JsonSerializerOptions
			{
				TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
				Converters =
				{
					JsonMetadataServices.StringConverter,
				},
			};

			var info = JsonTypeInfo.CreateJsonTypeInfo<string>(serializerOptions);

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
