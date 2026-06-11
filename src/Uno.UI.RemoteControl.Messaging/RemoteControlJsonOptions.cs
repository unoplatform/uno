#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Uno.UI.RemoteControl.Messaging;

/// <summary>
/// Shared <see cref="JsonSerializerOptions"/> for remote control message serialization.
/// </summary>
/// <remarks>
/// Supports a hybrid model: source-generated metadata for known message types
/// (zero reflection) with <see cref="DefaultJsonTypeInfoResolver"/> fallback for
/// external types that cannot register a <see cref="JsonSerializerContext"/>.
/// </remarks>
public static class RemoteControlJsonOptions
{
	private static JsonSerializerOptions? _options;

	public static JsonSerializerOptions Default => _options ??= CreateOptions(sourceGenerated: null);

	/// <summary>
	/// Registers a source-generated context to be used for known types.
	/// Must be called before the first serialization operation.
	/// Unknown types fall back to reflection-based resolution.
	/// </summary>
	public static void SetSourceGeneratedContext(JsonSerializerContext context)
	{
		_options = CreateOptions(context);
	}

	/// <summary>
	/// Drops the cached options + the registered source-generated context. A secondary (collectible-ALC)
	/// app's <see cref="RemoteControlClient"/> registers a per-ALC <see cref="JsonSerializerContext"/>
	/// (<c>RemoteControlJsonContext.Default</c>); holding it on this shared static pins that app's
	/// AssemblyLoadContext after unload. Called on app teardown; options are recreated lazily.
	/// </summary>
	public static void Reset() => _options = null;

	private static JsonSerializerOptions CreateOptions(JsonSerializerContext? sourceGenerated)
	{
		return new JsonSerializerOptions
		{
			PropertyNamingPolicy = null,
			PropertyNameCaseInsensitive = true,
			TypeInfoResolver = sourceGenerated is not null
				? JsonTypeInfoResolver.Combine(sourceGenerated, new DefaultJsonTypeInfoResolver())
				: new DefaultJsonTypeInfoResolver()
		};
	}
}
