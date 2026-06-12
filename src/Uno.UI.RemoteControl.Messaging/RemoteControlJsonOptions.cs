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
	private static JsonSerializerContext? _registeredContext;

	public static JsonSerializerOptions Default => _options ??= CreateOptions(_registeredContext);

	/// <summary>
	/// Registers a source-generated context to be used for known types.
	/// Must be called before the first serialization operation.
	/// Unknown types fall back to reflection-based resolution.
	/// </summary>
	public static void SetSourceGeneratedContext(JsonSerializerContext context)
	{
		_registeredContext = context;
		_options = CreateOptions(context);
	}

	/// <summary>
	/// Drops the cached options and — only when it is collectible — the registered
	/// source-generated context. A secondary (collectible-ALC) app's
	/// <see cref="RemoteControlClient"/> registers a per-ALC <see cref="JsonSerializerContext"/>
	/// (<c>RemoteControlJsonContext.Default</c>); holding it on this shared static pins that
	/// app's AssemblyLoadContext after unload. A host (non-collectible) context is preserved so
	/// surviving clients keep source-generated serialization instead of silently downgrading to
	/// reflection-based resolution (a perf and AOT/trimming hazard). Called on app teardown;
	/// options are recreated lazily.
	/// </summary>
	public static void Reset()
	{
#if NET5_0_OR_GREATER
		if (_registeredContext is { } context && context.GetType().IsCollectible)
		{
			_registeredContext = null;
		}
#else
		// Collectibility is not testable on this target; drop the context as before.
		_registeredContext = null;
#endif
		_options = null;
	}

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
