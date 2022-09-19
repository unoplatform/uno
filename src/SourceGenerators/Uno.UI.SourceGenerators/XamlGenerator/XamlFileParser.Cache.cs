extern alias __uno;
#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Uno.UI.SourceGenerators.XamlGenerator;

internal partial class XamlFileParser
{
	private static class Hash
	{
		/// <summary>
		/// The offset bias value used in the FNV-1a algorithm
		/// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
		/// </summary>
		internal const int FnvOffsetBias = unchecked((int)2166136261);

		/// <summary>
		/// The generative factor used in the FNV-1a algorithm
		/// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
		/// </summary>
		internal const int FnvPrime = 16777619;

		/// <summary>
		/// Compute the FNV-1a hash of a sequence of bytes
		/// See http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function
		/// </summary>
		/// <param name="data">The sequence of bytes</param>
		/// <returns>The FNV-1a hash of <paramref name="data"/></returns>
		internal static int GetFNVHashCode(ImmutableArray<byte> data)
		{
			int hashCode = Hash.FnvOffsetBias;

			for (int i = 0; i < data.Length; i++)
			{
				hashCode = unchecked((hashCode ^ data[i]) * Hash.FnvPrime);
			}

			return hashCode;
		}
	}

	private static class ByteSequenceComparer
	{
		// https://github.com/dotnet/roslyn/blob/6f47a0b611ab83ad1b7f8b404cc74098d37e283d/src/Compilers/Core/Portable/Collections/ByteSequenceComparer.cs#L24-L45
		internal static bool Equals(ImmutableArray<byte> x, ImmutableArray<byte> y)
		{
			if (x == y)
			{
				return true;
			}

			if (x.IsDefault || y.IsDefault || x.Length != y.Length)
			{
				return false;
			}

			for (var i = 0; i < x.Length; i++)
			{
				if (x[i] != y[i])
				{
					return false;
				}
			}

			return true;
		}

		// https://github.com/dotnet/roslyn/blob/6f47a0b611ab83ad1b7f8b404cc74098d37e283d/src/Compilers/Core/Portable/Collections/ByteSequenceComparer.cs#L101-L105
		internal static int GetHashCode(ImmutableArray<byte> x)
		{
			Debug.Assert(!x.IsDefault);
			return Hash.GetFNVHashCode(x);
		}
	}

	private struct CachedFileKey : IEquatable<CachedFileKey>
	{
		public CachedFileKey(string includeXamlNamespaces, string excludeXamlNamespaces, string file, ImmutableArray<byte> checksum)
		{
			IncludeXamlNamespaces = includeXamlNamespaces;
			ExcludeXamlNamespaces = excludeXamlNamespaces;
			File = file;
			Checksum = checksum;
		}

		public string IncludeXamlNamespaces { get; }
		public string ExcludeXamlNamespaces { get; }
		public string File { get; }
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
