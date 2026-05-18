#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	/// <summary>
	/// Value-equal description of a single XAML AdditionalText that flows through
	/// the incremental generator pipeline. Equality is structural so Roslyn can
	/// skip downstream per-file parsing entirely when the file (and its metadata)
	/// is unchanged across pipeline ticks.
	/// </summary>
	internal sealed record XamlSourceFile(
		string FilePath,
		string SourceLink,
		string TargetFilePath,
		string Content,
		ImmutableArray<byte> Checksum,
		bool IsApplicationDefinition,
		string GenerateCodeBehindOverride)
	{
		/// <summary>
		/// Quick textual probe for namespace conditionals that resolve against the
		/// current compilation (<c>?IsTypePresent(...)</c> / <c>?IsTypeNotPresent(...)</c>).
		/// Files matching this probe must take the compilation-aware parsing path so
		/// the inclusion decision can be evaluated against the Roslyn type table;
		/// files that don't match are safe to parse with no compilation input and
		/// can be Roslyn-cached across compilation changes.
		///
		/// Note: <c>?IsApiContractPresent</c>/<c>?IsApiContractNotPresent</c> is evaluated
		/// against a built-in contract version list inside <c>ApiInformation</c> and is
		/// independent of the compilation, so it does not require the slow path.
		/// </summary>
		public bool RequiresCompilationDuringParse { get; } =
			Content.IndexOf("?IsTypePresent", StringComparison.Ordinal) >= 0
			|| Content.IndexOf("?IsTypeNotPresent", StringComparison.Ordinal) >= 0;

		public bool Equals(XamlSourceFile? other)
			=> other is not null
				&& FilePath == other.FilePath
				&& SourceLink == other.SourceLink
				&& TargetFilePath == other.TargetFilePath
				&& IsApplicationDefinition == other.IsApplicationDefinition
				&& GenerateCodeBehindOverride == other.GenerateCodeBehindOverride
				&& ChecksumsEqual(Checksum, other.Checksum)
				&& Content == other.Content;

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = hash * 31 + FilePath.GetHashCode();
				hash = hash * 31 + SourceLink.GetHashCode();
				hash = hash * 31 + TargetFilePath.GetHashCode();
				hash = hash * 31 + IsApplicationDefinition.GetHashCode();
				hash = hash * 31 + GenerateCodeBehindOverride.GetHashCode();
				for (var i = 0; i < Checksum.Length; i++)
				{
					hash = hash * 31 + Checksum[i].GetHashCode();
				}
				return hash;
			}
		}

		private static bool ChecksumsEqual(ImmutableArray<byte> a, ImmutableArray<byte> b)
		{
			if (a.IsDefault && b.IsDefault)
			{
				return true;
			}

			if (a.IsDefault || b.IsDefault || a.Length != b.Length)
			{
				return false;
			}

			for (var i = 0; i < a.Length; i++)
			{
				if (a[i] != b[i])
				{
					return false;
				}
			}

			return true;
		}
	}

	/// <summary>
	/// Value-equal record of parser configuration extracted from MSBuild settings
	/// and the implicit-prefix list (which is cached separately so it survives
	/// most compilation changes).
	/// </summary>
	internal sealed record XamlParserSettings(
		string ExcludeXamlNamespacesProperty,
		string IncludeXamlNamespacesProperty,
		EquatableArray<string> ExcludeXamlNamespaces,
		EquatableArray<string> IncludeXamlNamespaces,
		bool EnableImplicitNamespaces,
		EquatableArray<(string Prefix, string Uri)> ImplicitPrefixes);

	/// <summary>
	/// <see cref="ImmutableArray{T}"/> wrapper that supplies structural equality.
	/// Required for any array-typed field on a record that participates in
	/// pipeline caching, since the default <see cref="ImmutableArray{T}"/> equality
	/// is by underlying-storage reference.
	/// </summary>
	internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>
		where T : IEquatable<T>
	{
		public ImmutableArray<T> Array { get; }

		public EquatableArray(ImmutableArray<T> array)
		{
			Array = array.IsDefault ? ImmutableArray<T>.Empty : array;
		}

		public int Length => Array.Length;

		public T this[int index] => Array[index];

		public ImmutableArray<T>.Enumerator GetEnumerator() => Array.GetEnumerator();

		public bool Equals(EquatableArray<T> other)
		{
			if (Array.Length != other.Array.Length)
			{
				return false;
			}

			for (var i = 0; i < Array.Length; i++)
			{
				if (!EqualityComparer<T>.Default.Equals(Array[i], other.Array[i]))
				{
					return false;
				}
			}

			return true;
		}

		public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				for (var i = 0; i < Array.Length; i++)
				{
					hash = hash * 31 + (Array[i]?.GetHashCode() ?? 0);
				}
				return hash;
			}
		}

		public static implicit operator EquatableArray<T>(ImmutableArray<T> array) => new(array);
	}

	/// <summary>
	/// Result of parsing a single XAML file. Carries the source descriptor so
	/// downstream pipeline nodes can correlate the parsed AST with its origin
	/// without re-querying MSBuild metadata.
	/// </summary>
	internal sealed record ParsedXamlFile(
		XamlSourceFile Source,
		XamlFileDefinition? Definition);
}
