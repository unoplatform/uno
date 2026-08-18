using System;
using System.Text.Json;
using Microsoft.UI.Xaml.Markup;

namespace Uno.UI.Markup;

#nullable enable

/// <summary>
/// A markup extension that converts a JSON payload into an <see cref="object"/> (typically an <see cref="System.Dynamic.ExpandoObject"/>)
/// so that it can be used directly from XAML resources or property setters.
/// </summary>
[ContentProperty(Name = nameof(Source))]
[MarkupExtensionReturnType(ReturnType = typeof(object))]
public sealed class FromJsonExtension : MarkupExtension
{
	/// <summary>
	/// Gets or sets the JSON document that should be converted into a dynamic object graph.
	/// </summary>
	public string? Source { get; set; }

	protected override object ProvideValue()
	{
		var jsonPayload = Source;
		if (string.IsNullOrWhiteSpace(jsonPayload))
		{
			throw new InvalidOperationException("FromJson requires a non-empty JSON payload.");
		}

		try
		{
#pragma warning disable CS8603 // A null JSON payload should remain null in the object graph.
			return JsonToObjectParser.Parse(jsonPayload!);
#pragma warning restore CS8603
		}
		catch (JsonException jsonEx)
		{
			throw new InvalidOperationException($"FromJson was unable to parse the provided JSON payload: {jsonEx.Message}", jsonEx);
		}
	}
}
