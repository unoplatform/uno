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
	/// <returns>The message posted from the WebView content to the host.</returns>
	/// <exception cref="T:System.ArgumentException">
	/// The message posted is some other kind of JavaScript type.
	/// </exception>
	public string TryGetWebMessageAsString()
	{
		if (string.IsNullOrWhiteSpace(WebMessageAsJson))
		{
			return WebMessageAsJson;
		}

		try
		{
			return JsonSerializer.Deserialize<string>(WebMessageAsJson, s_stringJsonTypeInfo);
		}
		catch (Exception)
		{
			if (this.Log().IsEnabled(LogLevel.Warning))
			{
				this.Log().Warn("The message could not be deserialized to a string.");
			}

			throw new ArgumentException("The message posted is some other kind of JavaScript type.");
		}
	}

	private static readonly JsonSerializerOptions s_serializerOptions = new JsonSerializerOptions
	{
		TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
		Converters =
		{
			JsonMetadataServices.StringConverter,
		},
	};

	private static readonly JsonTypeInfo<string> s_stringJsonTypeInfo = JsonTypeInfo.CreateJsonTypeInfo<string>(s_serializerOptions);

}
