using System;
using System.Globalization;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Uno.UI.RemoteControl.VS.Helpers;

// Based on https://github.com/dotnet/project-system/blob/9257cbdc5f5ca92d4c8a325e6b4c206220741e1a/src/Microsoft.VisualStudio.ProjectSystem.Managed.VS/ProjectSystem/VS/Reload/ProjectReloadManager.cs#L437
internal static class VsHierarchyExtensions
{
	public static T? GetProperty<T>(this IVsHierarchy hierarchy, VsHierarchyPropID property, T? defaultValue = default)
		=> hierarchy.GetProperty(-1, property, defaultValue);

	public static T? GetProperty<T>(this IVsHierarchy hierarchy, int item, VsHierarchyPropID property, T? defaultValue = default)
	{
		HResult hResult = hierarchy.GetProperty(item, property, defaultValue, out var result);
		if (hResult.Failed)
		{
			throw hResult.Exception ?? new Exception();
		}
		return result;
	}

	public static int GetProperty<T>(this IVsHierarchy hierarchy, int item, VsHierarchyPropID property, T? defaultValue, out T? result)
		=> hierarchy.GetPropertyCore(item, property, defaultValue, out result);

	private static int GetPropertyCore<T>(this IVsHierarchy hierarchy, int item, VsHierarchyPropID property, T? defaultValue, out T? result)
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
		if (hResult == -2147352573)
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
			throw new NotSupportedException("Unable to find parent");
		}

		var hierarchyItemId = hierarchy.GetProperty(VsHierarchyPropID.ParentHierarchyItemid, uint.MaxValue);

		if (hierarchyItemId == uint.MaxValue)
		{
			throw new NotSupportedException("Unable to find parent item");
		}
		ErrorHandler.ThrowOnFailure(((IVsPersistHierarchyItem2)parentHierarchy).ReloadItem((uint)hierarchyItemId, 0u));
	}
}
