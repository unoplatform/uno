using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.UI.Helpers;

/// <summary>
/// Helper class to map types to their original types and vice versa as part of hot reload.
/// </summary>
/// <remarks>
/// As of spec 041, this class is purely a mapping store. The pause/resume
/// surface (<see cref="Pause"/>, <see cref="Resume"/>, <see cref="IsPaused"/>,
/// <see cref="WaitForResume"/>) is kept for backward compatibility but is
/// inert: deferral of UI updates now lives in
/// <c>Uno.HotReload.Client.UIUpdate</c>.
/// </remarks>
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
	private static TypeMapCollection MappedTypeToOriginalTypeMappings { get; } = new();

	/// <summary>
	/// This maps an original type to the most recent replacement type. This dictionary will only grow when
	/// a different original type is modified.
	/// </summary>
	private static TypeMapCollection OriginalTypeToMappedType { get; } = new();

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
		MappedTypeToOriginalTypeMappings[mappedType] = originalType;
		OriginalTypeToMappedType[originalType] = mappedType;
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
	}

	private static int _obsoleteWarned;

	private static void WarnObsoleteOnce(string member)
	{
		if (Interlocked.Exchange(ref _obsoleteWarned, 1) != 0)
		{
			return;
		}

		// We don't have a logger reference here without dragging Uno.Foundation.Logging
		// into a dependency cycle for Uno.UI helpers — fall back to Console.Error so the
		// signal still surfaces in dev/CI logs. Fires at most once per process.
		try
		{
			Console.Error.WriteLine(
				$"[Uno.UI.Helpers.TypeMappings] WARNING: '{member}' is obsolete and now a no-op (spec 041). " +
				$"Use Uno.HotReload.Client.UIUpdate.Pause inside the relevant UpdateFile call instead.");
		}
		catch
		{
			// best-effort
		}
	}

	/// <summary>
	/// Returns a completed task — pause/resume is no longer implemented here.
	/// </summary>
	/// <remarks>
	/// Kept for backward compatibility with older external callers. Logs a
	/// one-time warning on first call.
	/// </remarks>
	[Obsolete("TypeMappings.WaitForResume is obsolete and always completes immediately. Use Uno.HotReload.Client.UIUpdate.Pause instead.", error: false)]
	public static Task WaitForResume()
	{
		WarnObsoleteOnce(nameof(WaitForResume));
		return Task.CompletedTask;
	}

	/// <summary>Always returns <see langword="false"/> — pause is no longer tracked here.</summary>
	[Obsolete("TypeMappings.IsPaused is obsolete and always returns false. Use Uno.HotReload.Client.UIUpdate.IsPaused instead.", error: false)]
	public static bool IsPaused
	{
		get
		{
			WarnObsoleteOnce(nameof(IsPaused));
			return false;
		}
	}

	/// <summary>No-op as of spec 041 — emits a one-time warning on first call.</summary>
	[Obsolete("TypeMappings.Pause is obsolete and now a no-op. Use Uno.HotReload.Client.UIUpdate.Pause instead.", error: false)]
	public static void Pause() => WarnObsoleteOnce(nameof(Pause));

	/// <summary>No-op as of spec 041 — emits a one-time warning on first call.</summary>
	[Obsolete("TypeMappings.Resume is obsolete and now a no-op. Use Uno.HotReload.Client.UIUpdate.Pause instead.", error: false)]
	public static void Resume() => WarnObsoleteOnce(nameof(Resume));
}

internal class TypeMapCollection
{
	private Dictionary<Type, Type> _mappings;

	public TypeMapCollection()
	{
		_mappings = new();
	}

	public TypeMapCollection(TypeMapCollection copy)
	{
		_mappings = new(copy._mappings);
	}

	public void Clear()
		=> _mappings.Clear();

	[DynamicallyAccessedMembers(TypeMappings.TypeRequirements)]
	public Type this[
		[DynamicallyAccessedMembers(TypeMappings.TypeRequirements)]
		Type key]
	{
		[UnconditionalSuppressMessage("Trimming", "IL2073", Justification = "Output must match Input, and since Input has annotations…")]
		get => _mappings[key];
		set => _mappings[key] = value;
	}

	[UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Output must match Input, and since Input has annotations…")]
	public bool TryGetValue(
		[DynamicallyAccessedMembers(TypeMappings.TypeRequirements)]
		Type key,
		[NotNullWhen(true)]
		[DynamicallyAccessedMembers(TypeMappings.TypeRequirements)]
		out Type value)
		=> _mappings.TryGetValue(key, out value);
}
