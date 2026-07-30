using System;
using System.Runtime.InteropServices;
using Uno.Foundation.Logging;

namespace Uno.UI.Runtime.Skia.Win32;

/// <summary>
/// Synthetic UIA pane provider that lives between the HWND root and the user
/// content. WinAppSDK's content-island host inserts two anonymous <c>pane</c>
/// nodes (DesktopChildSiteBridge + content island) above the Xaml content;
/// the Uno Skia Win32 host has no equivalent native chrome, so the structure
/// is synthesized in the managed UIA layer to preserve WinUI3 tree parity.
/// </summary>
/// <remarks>
/// These providers wrap no <c>UIElement</c> and own no automation peer — they
/// are purely structural. Children are resolved via the supplied delegates so
/// the inner pane can re-enter the root provider's normal child-discovery
/// logic at query time (allowing the user content to swap freely).
/// </remarks>
[ComVisible(true)]
internal sealed class Win32SyntheticPaneProvider :
	IRawElementProviderSimple,
	IRawElementProviderFragment
{
	private static int _nextRuntimeId = 100_000;

	private readonly nint _hwnd;
	private readonly Win32Accessibility _accessibility;
	private readonly Func<IRawElementProviderFragment?> _parentResolver;
	private readonly Func<IRawElementProviderFragment?> _firstChildResolver;
	private readonly Func<IRawElementProviderFragment?> _lastChildResolver;
	private readonly int _runtimeId;
	private readonly string _debugTag;

	internal Win32SyntheticPaneProvider(
		nint hwnd,
		Win32Accessibility accessibility,
		Func<IRawElementProviderFragment?> parentResolver,
		Func<IRawElementProviderFragment?> firstChildResolver,
		Func<IRawElementProviderFragment?> lastChildResolver,
		string debugTag)
	{
		_hwnd = hwnd;
		_accessibility = accessibility;
		_parentResolver = parentResolver;
		_firstChildResolver = firstChildResolver;
		_lastChildResolver = lastChildResolver;
		_runtimeId = System.Threading.Interlocked.Increment(ref _nextRuntimeId);
		_debugTag = debugTag;
	}

	// IRawElementProviderSimple

	public ProviderOptions ProviderOptions =>
		ProviderOptions.ServerSideProvider | ProviderOptions.UseComThreading;

	public object? GetPatternProvider(int patternId) => null;

	public object? GetPropertyValue(int propertyId) => propertyId switch
	{
		Win32UIAutomationInterop.UIA_NamePropertyId => string.Empty,
		Win32UIAutomationInterop.UIA_ControlTypePropertyId => Win32UIAutomationInterop.UIA_PaneControlTypeId,
		Win32UIAutomationInterop.UIA_LocalizedControlTypePropertyId => "pane",
		Win32UIAutomationInterop.UIA_ClassNamePropertyId => string.Empty,
		Win32UIAutomationInterop.UIA_FrameworkIdPropertyId => "Uno",
		Win32UIAutomationInterop.UIA_ProviderDescriptionPropertyId => "Uno Platform UIA Provider (synthetic pane)",
		Win32UIAutomationInterop.UIA_ProcessIdPropertyId => GetProcessId(),
		Win32UIAutomationInterop.UIA_NativeWindowHandlePropertyId => 0,
		Win32UIAutomationInterop.UIA_IsControlElementPropertyId => true,
		Win32UIAutomationInterop.UIA_IsContentElementPropertyId => false,
		Win32UIAutomationInterop.UIA_HasKeyboardFocusPropertyId => false,
		Win32UIAutomationInterop.UIA_IsKeyboardFocusablePropertyId => false,
		Win32UIAutomationInterop.UIA_IsEnabledPropertyId => true,
		Win32UIAutomationInterop.UIA_IsOffscreenPropertyId => false,
		Win32UIAutomationInterop.UIA_IsPasswordPropertyId => false,
		Win32UIAutomationInterop.UIA_IsRequiredForFormPropertyId => false,
		Win32UIAutomationInterop.UIA_IsDataValidForFormPropertyId => true,
		Win32UIAutomationInterop.UIA_IsDialogPropertyId => false,
		_ => null,
	};

	public IRawElementProviderSimple? HostRawElementProvider => null;

	// IRawElementProviderFragment

	public IRawElementProviderFragment? Navigate(NavigateDirection direction)
	{
		try
		{
			var result = direction switch
			{
				NavigateDirection.Parent => _parentResolver(),
				NavigateDirection.FirstChild => _firstChildResolver(),
				NavigateDirection.LastChild => _lastChildResolver(),
				NavigateDirection.NextSibling => null,
				NavigateDirection.PreviousSibling => null,
				_ => null,
			};

			if (this.Log().IsEnabled(LogLevel.Trace))
			{
				this.Log().Trace($"[UIA] Navigate({direction}) on synthetic pane '{_debugTag}' → {(result is null ? "(null)" : result.GetType().Name)}");
			}

			return result;
		}
		catch (Exception ex)
		{
			if (this.Log().IsEnabled(LogLevel.Debug))
			{
				this.Log().Debug($"Synthetic pane '{_debugTag}' Navigate({direction}) failed: {ex.Message}");
			}
			return null;
		}
	}

	public int[]? GetRuntimeId() => [Win32UIAutomationInterop.UiaAppendRuntimeId, _runtimeId];

	public UiaRect BoundingRectangle
	{
		get
		{
			// Synthetic panes cover the entire client area of the host HWND.
			// Delegating to the HWND root provider keeps the rect logic in one place
			// (transform composition + DPI scaling are handled there).
			return _accessibility.RootProvider?.BoundingRectangle ?? default;
		}
	}

	public IRawElementProviderFragment[]? GetEmbeddedFragmentRoots() => null;

	public void SetFocus() { }

	public IRawElementProviderFragmentRoot? FragmentRoot => _accessibility.RootProvider;

	private int GetProcessId()
	{
		_ = Win32UIAutomationInterop.GetWindowThreadProcessId(_hwnd, out var processId);
		return processId;
	}
}
