#nullable enable

extern alias __uno;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Uno.Extensions;

namespace Uno.UI.SourceGenerators.XamlGenerator;

internal partial class XamlCodeGeneration
{
	private static readonly ConcurrentDictionary<ResourceCacheKey, CachedResource> _cachedResources = new();
	private static readonly TimeSpan _cacheEntryLifetime = new TimeSpan(hours: 1, minutes: 0, seconds: 0);

	private static void ClearCache()
	{
		_cachedResources.Remove(kvp => DateTimeOffset.Now - kvp.Value.LastTimeUsed > _cacheEntryLifetime);
	}

	private struct ResourceCacheKey : IEquatable<ResourceCacheKey>
	{
		public ResourceCacheKey(string file, ImmutableArray<byte> checksum)
		{
			File = file;
			Checksum = checksum;
		}

		public string File { get; }
		public ImmutableArray<byte> Checksum { get; }

		public override bool Equals(object? obj)
			=> obj is ResourceCacheKey key && Equals(key);

		public bool Equals(ResourceCacheKey other)
			=> File == other.File && ByteSequenceComparer.Equals(Checksum, other.Checksum);

		public override int GetHashCode()
		{
			var hashCode = 682997901;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(File);
			hashCode = hashCode * -1521134295 + ByteSequenceComparer.GetHashCode(Checksum);
			return hashCode;
		}

		public static bool operator ==(ResourceCacheKey left, ResourceCacheKey right) => left.Equals(right);
		public static bool operator !=(ResourceCacheKey left, ResourceCacheKey right) => !(left == right);
	}

	private struct CachedResource : IEquatable<CachedResource>
	{
		public DateTimeOffset LastTimeUsed { get; }
		public string[] ResourceKeys { get; }

		public CachedResource(DateTimeOffset lastTimeUsed, string[] resourceKeys)
		{
			LastTimeUsed = lastTimeUsed;
			ResourceKeys = resourceKeys;
		}

		public CachedResource WithUpdatedLastTimeUsed()
		{
			return new CachedResource(DateTimeOffset.Now, ResourceKeys);
		}

		public override bool Equals(object? obj) => obj is CachedResource resource && Equals(resource);
		public bool Equals(CachedResource other) => LastTimeUsed.Equals(other.LastTimeUsed) && EqualityComparer<string[]>.Default.Equals(ResourceKeys, other.ResourceKeys);

		public override int GetHashCode()
		{
			var hashCode = 1975215354;
			hashCode = hashCode * -1521134295 + LastTimeUsed.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<string[]>.Default.GetHashCode(ResourceKeys);
			return hashCode;
		}

		public static bool operator ==(CachedResource left, CachedResource right) => left.Equals(right);
		public static bool operator !=(CachedResource left, CachedResource right) => !(left == right);
	}
}
