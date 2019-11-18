using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace Uno.UI.RemoteControl.HotReload.Messages
{
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

		public static Frame Read(Stream stream)
		{
			using (var reader = new BinaryReader(stream, Encoding.UTF8))
			{
				var version = reader.ReadInt16();
				var scope = reader.ReadString();
				var name = reader.ReadString();
				var content = reader.ReadString();

				return new Frame(version, scope, name, content);
			}
		}

		public static Frame Create<T>(short version, string scope, string name, T content)
			=> new Frame(
				version,
				scope,
				name,
				JsonConvert.SerializeObject(content)
			);

		public void WriteTo(Stream stream)
		{
			var writer = new BinaryWriter(stream, Encoding.UTF8);

			writer.Write((short)Version);
			writer.Write(Scope);
			writer.Write(Name);
			writer.Write(Content);
		}
	}
}
