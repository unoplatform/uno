using System;
using System.Globalization;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Uno.UI.RemoteControl.VS.Helpers;

// Based on https://github.com/dotnet/project-system/blob/9257cbdc5f5ca92d4c8a325e6b4c206220741e1a/src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Reload/ProjectReloadManager.cs#L437
internal static partial class VsHierarchyExtensions
{
	public static T? GetProperty<T>(this IVsHierarchy hierarchy, VsHierarchyPropID property, T? defaultValue = default(T?))
		=> hierarchy.GetProperty(HierarchyId.Root, property, defaultValue);

	public static HierarchyId GetProperty(this IVsHierarchy hierarchy, VsHierarchyPropID property, HierarchyId defaultValue = default(HierarchyId))
		=> hierarchy.GetProperty(HierarchyId.Root, property, defaultValue);

	public static T? GetProperty<T>(this IVsHierarchy hierarchy, HierarchyId item, VsHierarchyPropID property, T? defaultValue = default(T?))
	{
		HResult hResult = hierarchy.GetProperty(item, property, defaultValue, out var result);
		if (hResult.Failed)
		{
			throw hResult.Exception ?? new Exception();
		}
		return result;
	}

	public static HierarchyId GetProperty(this IVsHierarchy hierarchy, HierarchyId item, VsHierarchyPropID property, HierarchyId defaultValue = default(HierarchyId))
	{
		HResult hResult = hierarchy.GetProperty(item, property, defaultValue, out var result);
		if (hResult.Failed)
		{
			throw hResult.Exception ?? new Exception();
		}
		return result;
	}

	public static int GetProperty<T>(this IVsHierarchy hierarchy, HierarchyId item, VsHierarchyPropID property, T? defaultValue, out T? result)
		=> hierarchy.GetPropertyCore(item, property, defaultValue, out result);

	public static int GetProperty(this IVsHierarchy hierarchy, HierarchyId item, VsHierarchyPropID property, HierarchyId defaultValue, out HierarchyId result)
	{
		var propertyCore = hierarchy.GetPropertyCore(item, property, defaultValue.Id, out var rawResult);
		result = new HierarchyId(rawResult);
		return propertyCore;
	}

	private static int GetPropertyCore<T>(this IVsHierarchy hierarchy, HierarchyId item, VsHierarchyPropID property, T? defaultValue, out T? result)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		HResult hResult = hierarchy.GetProperty((uint)item, (int)property, out var obj);
		if (hResult.IsOK)
		{
			try
			{
				result = (T?)obj;
			}
			catch (InvalidCastException)
			{
				result = (T)Convert.ChangeType(obj, typeof(T), CultureInfo.InvariantCulture);
			}

			return HResult.OK;
		}
		if (hResult == HResult.MemberNotFound)
		{
			result = defaultValue;
			return HResult.OK;
		}
		result = default;
		return hResult;
	}

	internal static void ReloadProjectInSolution(this IVsHierarchy hierarchy)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		var parentHierarchy = hierarchy.GetProperty<IVsHierarchy>(VsHierarchyPropID.ParentHierarchy);

		if (parentHierarchy == null)
		{
			throw new InvalidOperationException("Unable to find hierarchy parent");
		}

		var parentItemId = hierarchy.GetProperty(VsHierarchyPropID.ParentHierarchyItemid, HierarchyId.Nil);

		if (parentItemId.IsNil)
		{
			throw new InvalidOperationException("Unable to find hierarchy parent item ID");
		}

		ErrorHandler.ThrowOnFailure(((IVsPersistHierarchyItem2)parentHierarchy).ReloadItem((uint)parentItemId, 0u));
	}
}
