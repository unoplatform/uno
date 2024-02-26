using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Helpers;

/// <summary>
/// Helper class to map types to their original types and vice versa as part of hot reload
/// </summary>
public static class TypeMappings
{
	/// <summary>
	/// This maps a replacement type to the original type. This dictionary will grow with each iteration 
	/// of the original type.
	/// </summary>
	private static IDictionary<Type, Type> AllMappedTypeToOriginalTypeMappings { get; } = new Dictionary<Type, Type>();

	/// <summary>
	/// This maps a replacement type to the original type. This dictionary will grow with each iteration 
	/// of the original type.
	/// Similar to AllMappedTypeToOriginalTypeMappings but doesn't update whilst hot reload is paused
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
	/// Similar to AllOriginalTypeToMappedType but doesn't update whilst hot reload is paused
	/// </summary>
	private static IDictionary<Type, Type> OriginalTypeToMappedType { get; set; } = new Dictionary<Type, Type>();

	/// <summary>
	/// Extension method to return the replacement type for a given instance type
	/// </summary>
	/// <param name="instanceType">This is the type that may have been replaced</param>
	/// <returns>If instanceType has been replaced, then the replacement type, otherwise the instanceType</returns>
	public static Type GetReplacementType(this Type instanceType)
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
	public static object CreateInstance<TOriginalType>()
		=> Activator.CreateInstance(typeof(TOriginalType).GetReplacementType());

	/// <summary>
	/// Creates an instance of the replacement type using its original type.
	/// </summary>
	/// <typeparam name="TOriginalType">The original type to be created</typeparam>
	/// <param name="args">The arguments used to create the instance, passed to the ctor</param>
	/// <returns>An new instance for the original type</returns>
	public static object CreateInstance<TOriginalType>(params object[] args)
		=> Activator.CreateInstance(typeof(TOriginalType).GetReplacementType(), args: args);

	internal static Type GetMappedType(this Type originalType) =>
		OriginalTypeToMappedType.TryGetValue(originalType, out var mappedType) ? mappedType : default;

	internal static Type GetOriginalType(this Type mappedType) =>
		MappedTypeToOriginalTypeMappings.TryGetValue(mappedType, out var originalType) ? originalType : default;

	internal static bool IsReplacedBy(this Type sourceType, Type mappedType)
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

	internal static void RegisterMapping(Type mappedType, Type originalType)
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

	private static TaskCompletionSource<bool> _mappingsPaused;

	/// <summary>
	/// Gets a Task that can be awaited to ensure type mappings
	/// are being applied. This is useful particularly for testing 
	/// HR the pause/resume function of type mappings
	/// </summary>
	/// <returns>A task that will complete when type mapping collection
	/// has resumed. Returns a completed task if type mapping collection
	/// is currently active.</returns>
	[Obsolete("Use WaitForResume instead")]
	public static Task WaitForMappingsToResume()
		=> WaitForResume();

	/// <summary>
	/// Gets a Task that can be awaited to ensure type mappings
	/// are being applied. This is useful particularly for testing 
	/// HR the pause/resume function of type mappings
	/// </summary>
	/// <returns>A task that will complete when type mapping collection
	/// has resumed. Returns a completed task if type mapping collection
	/// is currently active
	/// The value (bool) returned from the task indicates whether the layout should be updated</returns>
	public static Task<bool> WaitForResume()
		=> _mappingsPaused is not null ? _mappingsPaused.Task : Task.FromResult(true);


	/// <summary>
	/// Pause the collection of type mappings.
	/// Internally the type mappings are still collected but will only be
	/// applied to the mapping dictionaries after Resume is called
	/// </summary>
	public static void Pause()
		=> _mappingsPaused ??= new TaskCompletionSource<bool>();

	/// <summary>
	/// Resumes the collection of type mappings
	/// If new types have been created whilst type mapping
	/// was paused, those new mappings will be applied before
	/// the WaitForResume task completes
	/// </summary>
	public static void Resume()
		=> Resume(true);

	/// <summary>
	/// Resumes the collection of type mappings
	/// If new types have been created whilst type mapping
	/// was paused, those new mappings will be applied before
	/// the WaitForResume task completes
	/// </summary>
	/// <param name="updateLayout">Indicates whether the layout should be updated after resuming updates</param>
	public static void Resume(bool updateLayout)
	{
		var completion = _mappingsPaused;
		_mappingsPaused = null;
		if (completion is not null)
		{
			MappedTypeToOriginalTypeMappings = AllMappedTypeToOriginalTypeMappings.ToDictionary(x => x.Key, x => x.Value);
			OriginalTypeToMappedType = AllOriginalTypeToMappedType.ToDictionary(x => x.Key, x => x.Value);
			completion.TrySetResult(updateLayout);
		}
	}
}
