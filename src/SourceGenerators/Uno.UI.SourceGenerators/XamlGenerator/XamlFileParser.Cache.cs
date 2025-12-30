#nullable enable

extern alias __uno;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Uno.Equality;

namespace Uno.UI.SourceGenerators.XamlGenerator;

internal partial class XamlFileParser
{
	private struct CachedFileKey : IEquatable<CachedFileKey>
	{
		public CachedFileKey(string includeXamlNamespaces, string excludeXamlNamespaces, string file, string link, ImmutableArray<byte> checksum)
		{
			IncludeXamlNamespaces = includeXamlNamespaces;
			ExcludeXamlNamespaces = excludeXamlNamespaces;
			File = file;
			Link = link;
			Checksum = checksum;
		}

		public string IncludeXamlNamespaces { get; }
		public string ExcludeXamlNamespaces { get; }
		public string File { get; }
		public string Link { get; }
		public ImmutableArray<byte> Checksum { get; }

		public override bool Equals(object? obj) => obj is CachedFileKey key && Equals(key);

		public bool Equals(CachedFileKey other) =>
			IncludeXamlNamespaces == other.IncludeXamlNamespaces &&
			ExcludeXamlNamespaces == other.ExcludeXamlNamespaces &&
			File == other.File &&
			ByteSequenceComparer.Equals(Checksum, other.Checksum);

		public override int GetHashCode()
		{
			var hashCode = -145098327;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(IncludeXamlNamespaces);
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(ExcludeXamlNamespaces);
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(File);
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Link);
			hashCode = hashCode * -1521134295 + ByteSequenceComparer.GetHashCode(Checksum);
			return hashCode;
		}

		public static bool operator ==(CachedFileKey left, CachedFileKey right) => left.Equals(right);

		public static bool operator !=(CachedFileKey left, CachedFileKey right) => !(left == right);
	}

	private struct CachedFile : IEquatable<CachedFile>
	{
		public DateTimeOffset LastTimeUsed { get; }
		public XamlFileDefinition XamlFileDefinition { get; }

		public CachedFile(DateTimeOffset lastTimeUsed, XamlFileDefinition xamlFileDefinition)
		{
			LastTimeUsed = lastTimeUsed;
			XamlFileDefinition = xamlFileDefinition;
		}

		public CachedFile WithUpdatedLastTimeUsed()
		{
			return new CachedFile(DateTimeOffset.Now, XamlFileDefinition);
		}

		public override bool Equals(object? obj)
			=> obj is CachedFile file && Equals(file);

		public bool Equals(CachedFile other)
		{
			return LastTimeUsed.Equals(other.LastTimeUsed) &&
				EqualityComparer<XamlFileDefinition>.Default.Equals(XamlFileDefinition, other.XamlFileDefinition);
		}

		public override int GetHashCode()
		{
			var hashCode = 368878473;
			hashCode = hashCode * -1521134295 + LastTimeUsed.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<XamlFileDefinition>.Default.GetHashCode(XamlFileDefinition);
			return hashCode;
		}

		public static bool operator ==(CachedFile left, CachedFile right) => left.Equals(right);
		public static bool operator !=(CachedFile left, CachedFile right) => !(left == right);
	}
}
