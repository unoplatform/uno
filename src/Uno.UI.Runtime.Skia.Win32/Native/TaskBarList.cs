using Uno.Foundation.Logging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Helper class for interacting with the Windows taskbar via ITaskbarList3.
/// </summary>
internal static class TaskBarList
{
	private static ITaskbarList3* _taskbarList;
	private static bool _initialized;
	private static readonly object _lock = new();

	/// <summary>
	/// Sets or clears an overlay icon on the taskbar button for the specified window.
	/// When called with null icon, this forces the taskbar to refresh the window's icon.
	/// </summary>
	/// <param name="hwnd">Handle to the window.</param>
	/// <param name="hIcon">Handle to the overlay icon, or HICON.Null to clear.</param>
	/// <param name="description">Accessibility description, or null.</param>
	public static unsafe void SetOverlayIcon(HWND hwnd, HICON hIcon, string? description)
	{
		if (!EnsureInitialized())
		{
			return;
		}

		HRESULT hr;
		fixed (char* pDescription = description)
		{
			hr = _taskbarList->SetOverlayIcon(hwnd, hIcon, new PCWSTR(pDescription));
		}

		if (hr.Failed)
		{
			typeof(TaskBarList).LogDebug()?.Debug($"{nameof(ITaskbarList3.SetOverlayIcon)} failed: {Win32Helper.GetErrorMessage((uint)hr.Value)}");
		}
	}

	private static unsafe bool EnsureInitialized()
	{
		if (_initialized)
		{
			return _taskbarList != null;
		}

		lock (_lock)
		{
			if (_initialized)
			{
				return _taskbarList != null;
			}

			_initialized = true;

			var taskbarListClsid = CLSID.TaskbarList;
			var taskbarListIid = ITaskbarList3.IID_Guid;
			ITaskbarList3* taskbarList;

			var hr = PInvoke.CoCreateInstance(
				&taskbarListClsid,
				null,
				CLSCTX.CLSCTX_INPROC_SERVER,
				&taskbarListIid,
				(void**)&taskbarList);

			if (hr.Failed)
			{
				typeof(TaskBarList).LogDebug()?.Debug($"Failed to create ITaskbarList3: {Win32Helper.GetErrorMessage((uint)hr.Value)}");
				return false;
			}

			hr = taskbarList->HrInit();
			if (hr.Failed)
			{
				typeof(TaskBarList).LogDebug()?.Debug($"ITaskbarList3.HrInit failed: {Win32Helper.GetErrorMessage((uint)hr.Value)}");
				taskbarList->Release();
				return false;
			}

			_taskbarList = taskbarList;
			return true;
		}
	}
}
