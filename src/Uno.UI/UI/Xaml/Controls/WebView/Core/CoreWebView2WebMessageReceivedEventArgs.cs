using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Uno.Foundation.Logging;

namespace Microsoft.Web.WebView2.Core;

/// <summary>
/// Event args for the WebMessageReceived event.
/// </summary>
public partial class CoreWebView2WebMessageReceivedEventArgs
{
	// Null when the platform does not report the source; the file is nullable-oblivious.
	private readonly string _source;

	internal CoreWebView2WebMessageReceivedEventArgs(string webMessageAsJson, string source = null)
	{
		WebMessageAsJson = webMessageAsJson;
		_source = source;
	}

	/// <summary>
	/// Gets the message posted from the WebView content to the host converted to a JSON string.
	/// </summary>
	public string WebMessageAsJson { get; }

	/// <summary>
	/// Gets the URI of the document that sent this web message.
	/// </summary>
	/// <exception cref="NotImplementedException">The native WebView of this platform does not report the message source.</exception>
	public string Source
		=> _source ?? throw global::Windows.Foundation.Metadata.ApiInformation.CreateNotImplementedException("Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs", "Source");

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
			return JsonSerializer.Deserialize<string>(WebMessageAsJson, CoreWebView2WebMessageReceivedEventArgsJsonSerializerContext.Default.String);
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
}

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(string))]
internal sealed partial class CoreWebView2WebMessageReceivedEventArgsJsonSerializerContext : JsonSerializerContext
{
}
