using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Helpers;

/// <summary>
/// Helper class to map types to their original types and vice versa as part of hot reload
/// </summary>
public static class TypeMappings
{
	internal const DynamicallyAccessedMemberTypes TypeRequirements =
		  DynamicallyAccessedMemberTypes.PublicParameterlessConstructor
		| DynamicallyAccessedMemberTypes.PublicConstructors
		;

	/// <summary>
	/// This maps a replacement type to the original type. This dictionary will grow with each iteration 
	/// of the original type.
	/// </summary>
	private static IDictionary<Type, Type> AllMappedTypeToOriginalTypeMappings { get; } = new Dictionary<Type, Type>();

	/// <summary>
	/// This maps a replacement type to the original type. This dictionary will grow with each iteration 
	/// of the original type.
	/// Similiar to AllMappedTypeToOriginalTypeMappings but doesn't update whilst hot reload is paused
	/// </summary>
	private static IDictionary<Type, Type> MappedTypeToOriginalTypeMappings { get; set; } = new Dictionary<Type, Type>();

	/// <summary>
	/// This maps an original type to the most recent replacement type. This dictionary will only grow when
	/// a different original type is modified.
	/// </summary>
	private static IDictionary<Type, Type> AllOriginalTypeToMappedType { get; } = new Dictionary<Type, Type>();

	/// <summary>
	/// This maps an original type to the most recent replacement type. This dictionary will only grow when
	/// a different original type is modified.
	/// Similiar to AllOriginalTypeToMappedType but doesn't update whilst hot reload is paused
	/// </summary>
	private static IDictionary<Type, Type> OriginalTypeToMappedType { get; set; } = new Dictionary<Type, Type>();

	/// <summary>
	/// Extension method to return the replacement type for a given instance type
	/// </summary>
	/// <param name="instanceType">This is the type that may have been replaced</param>
	/// <returns>If instanceType has been replaced, then the replacement type, otherwise the instanceType</returns>
	[return: DynamicallyAccessedMembers(TypeRequirements)]
	public static Type GetReplacementType(
		[DynamicallyAccessedMembers(TypeRequirements)] this Type instanceType)
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
	/// <typeparam name="TOriginalType">The original type to be created</typeparam>
	/// <returns>An new instance for the original type</returns>
	[UnconditionalSuppressMessage("Trimming", "IL2072", Justification = "Types manipulated here have been marked earlier")]
	public static object CreateInstance<[DynamicallyAccessedMembers(TypeRequirements)] TOriginalType>()
		=> Activator.CreateInstance(typeof(TOriginalType).GetReplacementType());

	/// <summary>
	/// Creates an instance of the replacement type using its original type.
	/// </summary>
	/// <typeparam name="TOriginalType">The original type to be created</typeparam>
	/// <param name="args">The arguments used to create the instance, passed to the ctor</param>
	/// <returns>An new instance for the original type</returns>
	public static object CreateInstance<[DynamicallyAccessedMembers(TypeRequirements)] TOriginalType>(params object[] args)
		=> Activator.CreateInstance(typeof(TOriginalType).GetReplacementType(), args: args);

	[return: DynamicallyAccessedMembers(TypeRequirements)]
	internal static Type GetMappedType(
		[DynamicallyAccessedMembers(TypeRequirements)]
		this Type originalType) =>
		OriginalTypeToMappedType.TryGetValue(originalType, out var mappedType) ? mappedType : default;

	[return: DynamicallyAccessedMembers(TypeRequirements)]
	internal static Type GetOriginalType(
		[DynamicallyAccessedMembers(TypeRequirements)]
		this Type mappedType) =>
		MappedTypeToOriginalTypeMappings.TryGetValue(mappedType, out var originalType) ? originalType : default;

	internal static bool IsReplacedBy(
		[DynamicallyAccessedMembers(TypeRequirements)]
		this Type sourceType,
		[DynamicallyAccessedMembers(TypeRequirements)]
		Type mappedType)
	{
		if (mappedType.GetOriginalType() is { } originalType)
		{
			if (originalType == sourceType)
			{
				return true;
			}
			return sourceType.GetOriginalType()?.GetMappedType() == mappedType;
		}

		return false;
	}

	internal static void RegisterMapping(
		[DynamicallyAccessedMembers(TypeRequirements)]
		Type mappedType,
		[DynamicallyAccessedMembers(TypeRequirements)]
		Type originalType)
	{
		AllMappedTypeToOriginalTypeMappings[mappedType] = originalType;
		AllOriginalTypeToMappedType[originalType] = mappedType;

		if (_mappingsPaused is null)
		{
			MappedTypeToOriginalTypeMappings[mappedType] = originalType;
			OriginalTypeToMappedType[originalType] = mappedType;
		}
	}

	/// <summary>
	/// This method is required for testing purposes. A typical test will navigate
	/// to a specific page and expect that page (not a replacement type) as a starting 
	/// point. For this to work, we need to reset the mappings.
	/// </summary>
	internal static void ClearMappings()
	{
		MappedTypeToOriginalTypeMappings.Clear();
		OriginalTypeToMappedType.Clear();
		AllMappedTypeToOriginalTypeMappings.Clear();
		AllOriginalTypeToMappedType.Clear();
	}

	private static TaskCompletionSource _mappingsPaused;

	/// <summary>
	/// Gets a Task that can be awaited to ensure type mappings
	/// are being applied. This is useful particularly for testing 
	/// HR the pause/resume function of type mappings
	/// </summary>
	/// <returns>A task that will complete when type mapping collection
	/// has resumed. Returns a completed task if type mapping collection
	/// is currently active
	/// The value (bool) returned from the task indicates whether the layout should be updated</returns>
	public static Task WaitForResume()
		=> _mappingsPaused is not null ? _mappingsPaused.Task : Task.FromResult(true);

	/// <summary>
	/// Gets whether type mappings are currently paused 
	/// </summary>
	public static bool IsPaused => _mappingsPaused is not null;


	/// <summary>
	/// Pause the collection of type mappings.
	/// Internally the type mappings are still collected but will only be
	/// applied to the mapping dictionaries after Resume is called
	/// </summary>
	public static void Pause() => Interlocked.CompareExchange(ref _mappingsPaused, new TaskCompletionSource(), null);

	/// <summary>
	/// Resumes the collection of type mappings
	/// If new types have been created whilst type mapping
	/// was paused, those new mappings will be applied before
	/// the WaitForResume task completes
	/// </summary>
	public static void Resume()
	{
		if (Interlocked.Exchange(ref _mappingsPaused, null) is { } completion)
		{
			MappedTypeToOriginalTypeMappings = new Dictionary<Type, Type>(AllMappedTypeToOriginalTypeMappings);
			OriginalTypeToMappedType = new Dictionary<Type, Type>(AllOriginalTypeToMappedType);
			completion.TrySetResult();
		}
	}
}
