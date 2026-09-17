#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Uno.UI.SourceGenerators.DependencyObject.Models;
using Uno.UI.SourceGenerators.Internal.Incremental;

namespace Uno.UI.SourceGenerators.DependencyObject;

/// <summary>
/// Implements partial properties and partial static <c>Get{Name}</c>/<c>Set{Name}</c> methods marked with
/// <c>[GeneratedDependencyProperty]</c>, and declares their <c>{Name}Property</c> identifiers.
/// </summary>
[Generator]
public sealed class DependencyPropertyGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var results = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				DependencyPropertyModelBuilder.AttributeMetadataName,
				static (node, _) => node is PropertyDeclarationSyntax or IndexerDeclarationSyntax or MethodDeclarationSyntax,
				static (context, cancellationToken) => DependencyPropertyModelBuilder.Build(context, cancellationToken))
			.WithTrackingName(TrackingNames.Results);

		var diagnostics = results
			.Select(static (result, _) => result.Diagnostics)
			.Where(static diagnostics => diagnostics.Count > 0)
			.WithTrackingName(TrackingNames.Diagnostics);

		context.RegisterSourceOutput(diagnostics, static (context, diagnostics) =>
		{
			foreach (var diagnostic in diagnostics)
			{
				if (DependencyPropertyDiagnostics.ById.TryGetValue(diagnostic.DescriptorId, out var descriptor))
				{
					context.ReportDiagnostic(diagnostic.ToDiagnostic(descriptor));
				}
			}
		});

		var types = results
			.Where(static result => result.Property is not null)
			.Collect()
			.SelectMany(static (results, cancellationToken) => GroupByContainingType(results, cancellationToken))
			.WithTrackingName(TrackingNames.Types);

		var boxes = context.CompilationProvider
			.Select(static (compilation, _) => GetBoxesInfo(compilation))
			.WithTrackingName(TrackingNames.Boxes);

		context.RegisterSourceOutput(types.Combine(boxes), static (context, source) =>
		{
			var (type, boxes) = source;
			context.AddSource(type.HintName, DependencyPropertyEmitter.Emit(type, boxes));
		});
	}

	private static ImmutableArray<DependencyPropertyTypeInfo> GroupByContainingType(ImmutableArray<DependencyPropertyResult> results, CancellationToken cancellationToken)
	{
		var groups = new Dictionary<string, (HierarchyInfo ContainingType, ImmutableArray<DependencyPropertyInfo>.Builder Properties)>(StringComparer.Ordinal);
		var order = new List<string>();

		foreach (var result in results)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var key = result.ContainingType.MetadataName;
			if (!groups.TryGetValue(key, out var group))
			{
				group = (result.ContainingType, ImmutableArray.CreateBuilder<DependencyPropertyInfo>());
				groups.Add(key, group);
				order.Add(key);
			}

			group.Properties.Add(result.Property!);
		}

		var hintNames = GetHintNames(order);

		return order
			.Select((key, index) => new DependencyPropertyTypeInfo(groups[key].ContainingType, hintNames[index], new EquatableArray<DependencyPropertyInfo>(groups[key].Properties.ToImmutable())))
			.ToImmutableArray();
	}

	/// <summary>
	/// Hint names must be unique ignoring case, so the names of types that differ only by case get a suffix. It's derived
	/// from the exact metadata name, so it doesn't depend on the order of the types.
	/// </summary>
	private static string[] GetHintNames(List<string> metadataNames)
	{
		var baseNames = metadataNames.Select(name => name.Replace('`', '-').Replace('+', '.')).ToArray();
		var collisions = baseNames
			.GroupBy(name => name, StringComparer.OrdinalIgnoreCase)
			.Where(group => group.Count() > 1)
			.Select(group => group.Key)
			.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

		var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var hintNames = new string[baseNames.Length];
		for (var i = 0; i < baseNames.Length; i++)
		{
			var name = collisions.Contains(baseNames[i]) ? $"{baseNames[i]}.{GetStableHash(metadataNames[i]):X8}" : baseNames[i];

			var hintName = name + ".g.cs";
			for (var suffix = 2; !used.Add(hintName); suffix++)
			{
				hintName = $"{name}.{suffix}.g.cs";
			}

			hintNames[i] = hintName;
		}

		return hintNames;
	}

	private static uint GetStableHash(string value)
	{
		// FNV-1a, since string.GetHashCode differs between processes.
		var hash = 2166136261u;
		foreach (var c in value)
		{
			hash = (hash ^ c) * 16777619u;
		}

		return hash;
	}

	/// <summary>
	/// The boxes are internal to Uno.UI, so other compilations get unboxed code rather than failing to compile. The
	/// accessibility check matters because referenced Uno packages contain an inaccessible copy.
	/// </summary>
	private static BoxesInfo GetBoxesInfo(Compilation compilation)
	{
		if (compilation.GetTypeByMetadataName(BoxesInfo.BoxerMetadataName) is not { } boxer ||
			!compilation.IsSymbolAccessibleWithin(boxer, compilation.Assembly))
		{
			return new BoxesInfo(IsAccessible: false, EquatableArray<string>.Empty, EquatableArray<string>.Empty);
		}

		var boxableTypes = boxer.GetMembers("Box")
			.OfType<IMethodSymbol>()
			.Where(m => m is { IsStatic: true, IsGenericMethod: false, Parameters.Length: 1, ReturnType.SpecialType: SpecialType.System_Object } && m.Parameters[0].RefKind == RefKind.None)
			.Select(m => m.Parameters[0].Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat))
			.OrderBy(name => name, StringComparer.Ordinal)
			.ToEquatableArray();

		// The holders (BoolBoxes, IntBoxes, ...) are siblings of Boxer, in the same assembly.
		var cachedBoxes = boxer.ContainingNamespace.GetTypeMembers()
			.Where(holder => compilation.IsSymbolAccessibleWithin(holder, compilation.Assembly))
			.SelectMany(holder => holder.GetMembers()
				.OfType<IFieldSymbol>()
				.Where(f => f.IsStatic && f.Type.SpecialType == SpecialType.System_Object && compilation.IsSymbolAccessibleWithin(f, compilation.Assembly))
				.Select(f => $"{holder.Name}.{f.Name}"))
			.OrderBy(name => name, StringComparer.Ordinal)
			.ToEquatableArray();

		return new BoxesInfo(IsAccessible: true, boxableTypes, cachedBoxes);
	}

	/// <summary>
	/// Step names for incremental generator tests.
	/// </summary>
	internal static class TrackingNames
	{
		public const string Results = nameof(Results);
		public const string Diagnostics = nameof(Diagnostics);
		public const string Types = nameof(Types);
		public const string Boxes = nameof(Boxes);
	}
}
