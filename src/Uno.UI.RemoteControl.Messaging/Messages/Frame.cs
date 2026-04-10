#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Uno.UI.RemoteControl.HotReload.Messages;

[DebuggerDisplay("{Name}-{Scope}")]
public class Frame
{
	// Assembly.IsCollectible is not available on netstandard2.0; cache the PropertyInfo once.
	private static readonly System.Reflection.PropertyInfo? _isCollectibleProp =
		typeof(System.Reflection.Assembly).GetProperty("IsCollectible");

	private static readonly bool _isRunningInCollectibleAlc =
		_isCollectibleProp?.GetValue(typeof(Frame).Assembly) is true;

	/// <summary>
	/// When running inside a collectible ALC, returns settings with a per-call
	/// <see cref="DefaultContractResolver"/> so compiled <c>DynamicMethod</c> delegates
	/// don't accumulate in the global static resolver (where they pin the ALC's
	/// <c>LoaderAllocator</c> and prevent collection).
	/// In the default ALC the global resolver is used for normal caching/performance.
	/// </summary>
	private static JsonSerializerSettings? GetSettingsForCurrentContext()
	{
		if (!_isRunningInCollectibleAlc)
		{
			return null;
		}

		return new JsonSerializerSettings
		{
			ContractResolver = new DefaultContractResolver()
		};
	}

	public Frame(short version, string scope, string name, string content)
	{
		Version = version;
		Scope = scope;
		Name = name;
		Content = content;
	}

	public int Version { get; }

	public string Scope { get; }

	public string Name { get; }

	public string Content { get; }

	public static Frame Create<T>(short version, string scope, string name, T content)
		=> new Frame(
			version,
			scope,
			name,
			JsonConvert.SerializeObject(content, GetSettingsForCurrentContext())
		);

	public T GetContent<T>()
		=> TryGetContent<T>(out var content) ? content : throw new InvalidOperationException("Invalid frame");

	public bool TryGetContent<T>([NotNullWhen(true)] out T? content)
	{
		try
		{
			content = JsonConvert.DeserializeObject<T>(Content, GetSettingsForCurrentContext());
			if (content is not null)
			{
				return true;
			}
			else
			{
				DevServerDiagnostics.Current.ReportInvalidFrame<T>(this);
				return false;
			}
		}
		catch (Exception)
		{
			DevServerDiagnostics.Current.ReportInvalidFrame<T>(this);
			content = default;
			return false;
		}
	}

	public static Frame Read(Stream stream)
	{
		using var reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);

		var version = reader.ReadInt16();
		var scope = reader.ReadString();
		var name = reader.ReadString();
		var content = reader.ReadString();

		return new Frame(version, scope, name, content);
	}

	public void WriteTo(Stream stream)
	{
		using var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

		writer.Write((short)Version);
		writer.Write(Scope);
		writer.Write(Name);
		writer.Write(Content);

		writer.Flush();
	}
}
