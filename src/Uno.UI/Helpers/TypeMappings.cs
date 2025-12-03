#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Uno.UI.Helpers;

/// <summary>
/// Helper class to map types to their original types and vice versa as part of hot reload
/// </summary>
public static class TypeMappings
{
	/// <summary>
	/// This maps a replacement type to the original type. This dictionary will grow with each iteration of the original type.
	/// </summary>
	private static ImmutableDictionary<Type, Type> _mappedTypeToOriginalType = ImmutableDictionary<Type, Type>.Empty;

	/// <summary>
	/// This maps an original type to the most recent replacement type. This dictionary will only grow when
	/// a different original type is modified.
	/// </summary>
	private static ImmutableDictionary<Type, Type> _originalTypeToMappedType = ImmutableDictionary<Type, Type>.Empty;

	/// <summary>
	/// Extension method to return the replacement type for a given instance type
	/// </summary>
	/// <param name="instanceType">This is the type (original or any updated version of it) that may have been replaced</param>
	/// <returns>If instanceType has been replaced, then the replacement type, otherwise the instanceType</returns>
	public static Type GetReplacementType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] this Type instanceType)
	{
		// Two scenarios:
		// 1. The instance type is a mapped type, in which case we need to get the original type
		// 2. The instance type is an original type, in which case we need to get the mapped type
		var originalType = GetOriginalType(instanceType) ?? instanceType;
		return originalType.GetMappedType() ?? instanceType;
	}

	/// <summary>
	/// Creates an instance of the replacement type using its original type.
	/// </summary>
	/// <typeparam name="TType">The type (original or any updated version of it) to be created</typeparam>
	/// <returns>A new instance for the original type</returns>
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Types manipulated here have been marked earlier")]
	public static object? CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TType>()
		=> Activator.CreateInstance(typeof(TType).GetReplacementType());

	/// <summary>
	/// Creates an instance of the replacement type using its original type.
	/// </summary>
	/// <typeparam name="TType">The type (original or any updated version of it) to be created</typeparam>
	/// <param name="args">The arguments used to create the instance, passed to the ctor</param>
	/// <returns>A new instance for the original type</returns>
	public static object? CreateInstance<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TType>(params object[] args)
		=> Activator.CreateInstance(typeof(TType).GetReplacementType(), args: args);

	internal static Type? GetMappedType(this Type originalType)
		=> _originalTypeToMappedType.GetValueOrDefault(originalType);

	internal static Type? GetOriginalType(this Type mappedType)
		=> _mappedTypeToOriginalType.GetValueOrDefault(mappedType);

	internal static void RegisterMappings(ImmutableDictionary<Type, Type> updatedTypesPerOriginal)
	{
		ImmutableInterlocked.Update(
			ref _mappedTypeToOriginalType,
			static (all, updated) => all.AddRange(updated.Select(pair => new KeyValuePair<Type, Type>(pair.Value, pair.Key))),
			updatedTypesPerOriginal);

		ImmutableInterlocked.Update(
			ref _originalTypeToMappedType,
			static (all, updated) => all.SetItems(updated),
			updatedTypesPerOriginal);
	}
}
