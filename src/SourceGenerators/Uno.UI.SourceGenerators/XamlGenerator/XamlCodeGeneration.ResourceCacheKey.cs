#nullable enable

extern alias __uno;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using Uno.Equality;
using Uno.Extensions;

namespace Uno.UI.SourceGenerators.XamlGenerator;

internal partial class XamlCodeGeneration
{
	private static readonly ConcurrentDictionary<ResourceCacheKey, CachedResource> _cachedResources = new();

	// The cache entry of the last version of each resource file that parsed, by assembly and file path. The cache
	// never evicts those entries: a parse failure falls back to them, however long since the file was last used.
	private static readonly ConcurrentDictionary<(string Assembly, string File), ResourceCacheKey> _lastGoodResources = new();

	// Settable for tests.
	internal static TimeSpan CacheEntryLifetime { get; set; } = new TimeSpan(hours: 1, minutes: 0, seconds: 0);

	private static void ScavengeCache()
		=> _cachedResources.Remove(kvp => DateTimeOffset.Now - kvp.Value.LastTimeUsed > CacheEntryLifetime && !IsLastGood(kvp.Key));

	private static bool IsLastGood(ResourceCacheKey key)
		=> _lastGoodResources.TryGetValue(key.FileId, out var lastGood) && lastGood == key;

	private struct ResourceCacheKey : IEquatable<ResourceCacheKey>
	{
		public ResourceCacheKey(string assembly, string file, ImmutableArray<byte> checksum)
		{
			Assembly = assembly;
			File = file;
			Checksum = checksum;
		}

		// The keys embed the assembly name, so the same file content must not be shared across assemblies.
		public string Assembly { get; }
		public string File { get; }
		public ImmutableArray<byte> Checksum { get; }

		public (string Assembly, string File) FileId => (Assembly, File);

		public override bool Equals(object? obj)
			=> obj is ResourceCacheKey key && Equals(key);

		public bool Equals(ResourceCacheKey other)
			=> Assembly == other.Assembly && File == other.File && ByteSequenceComparer.Equals(Checksum, other.Checksum);

		public override int GetHashCode()
		{
			var hashCode = 682997901;
			hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Assembly);
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
		public ResourceDetails[] ResourceKeys { get; }

		public CachedResource(DateTimeOffset lastTimeUsed, ResourceDetails[] resourceKeys)
		{
			LastTimeUsed = lastTimeUsed;
			ResourceKeys = resourceKeys;
		}

		public CachedResource WithUpdatedLastTimeUsed()
		{
			return new CachedResource(DateTimeOffset.Now, ResourceKeys);
		}

		public override bool Equals(object? obj) => obj is CachedResource resource && Equals(resource);
		public bool Equals(CachedResource other) => LastTimeUsed.Equals(other.LastTimeUsed) && EqualityComparer<ResourceDetails[]>.Default.Equals(ResourceKeys, other.ResourceKeys);

		public override int GetHashCode()
		{
			var hashCode = 1975215354;
			hashCode = hashCode * -1521134295 + LastTimeUsed.GetHashCode();
			hashCode = hashCode * -1521134295 + EqualityComparer<ResourceDetails[]>.Default.GetHashCode(ResourceKeys);
			return hashCode;
		}

		public static bool operator ==(CachedResource left, CachedResource right) => left.Equals(right);
		public static bool operator !=(CachedResource left, CachedResource right) => !(left == right);
	}
}
