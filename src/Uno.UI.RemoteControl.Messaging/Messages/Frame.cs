#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Uno.UI.RemoteControl.HotReload.Messages;

[DebuggerDisplay("{Name}-{Scope}")]
public class Frame
{
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
			JsonConvert.SerializeObject(content)
		);

	public T GetContent<T>()
		=> TryGetContent<T>(out var content) ? content : throw new InvalidOperationException("Invalid frame");

	public bool TryGetContent<T>([NotNullWhen(true)] out T? content)
	{
		try
		{
			content = JsonConvert.DeserializeObject<T>(Content);
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
