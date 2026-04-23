#if HAS_UNO
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Tests.Common;
using Private.Infrastructure;
using Uno.UI.Core;
using Uno.UI.Xaml.Core;
using Windows.System;
using Windows.UI.Core;

namespace Uno.UI.RuntimeTests.MUX.Input.KeyboardAccelerators;

/// <summary>
/// Diagnostic helpers used to surface state when keyboard-accelerator tests
/// fail waiting for the <c>Invoked</c> event. The goal is to make the
/// assertion message actionable by including focus / visual-tree / keyboard
/// state at the exact moment the timeout fires.
/// </summary>
internal static class KeyboardAcceleratorTestDiagnostics
{
	private static readonly VirtualKey[] _trackedKeys =
	{
		VirtualKey.Control, VirtualKey.LeftControl, VirtualKey.RightControl,
		VirtualKey.Shift, VirtualKey.LeftShift, VirtualKey.RightShift,
		VirtualKey.Menu, VirtualKey.LeftMenu, VirtualKey.RightMenu,
		VirtualKey.LeftWindows, VirtualKey.RightWindows,
	};

	/// <summary>
	/// Awaits an <see cref="EventTester{TSender,TEventArgs}"/> Invoked wait,
	/// and if it times out, enriches the failure with detailed diagnostic info.
	/// </summary>
	public static async Task WaitWithDiagnosticsAsync<TSender, TArgs>(
		this EventTester<TSender, TArgs> tester,
		UIElement expectedFocusedElement,
		KeyboardAccelerator expectedAccelerator,
		string context,
		TimeSpan? timeout = null)
	{
		var actualTimeout = timeout ?? TimeSpan.FromSeconds(5);
		var beforeSnapshot = await CaptureSnapshotAsync("before-wait", expectedFocusedElement, expectedAccelerator);

		var fired = await tester.WaitForNoThrow(actualTimeout);
		if (fired)
		{
			return;
		}

		var afterSnapshot = await CaptureSnapshotAsync("after-timeout", expectedFocusedElement, expectedAccelerator);

		var diag = new StringBuilder();
		diag.AppendLine($"Timed out waiting for Invoked event. Context: {context}");
		diag.AppendLine("=== BEFORE WAIT ===");
		diag.AppendLine(beforeSnapshot);
		diag.AppendLine("=== AFTER TIMEOUT ===");
		diag.AppendLine(afterSnapshot);

		throw new TimeoutException(diag.ToString());
	}

	/// <summary>
	/// Capture a snapshot of focus, XamlRoot, keyboard modifier state and
	/// live keyboard accelerator registrations. Runs on the UI thread since
	/// many of these APIs require it.
	/// </summary>
	public static async Task<string> CaptureSnapshotAsync(
		string label,
		UIElement expectedFocusedElement,
		KeyboardAccelerator expectedAccelerator)
	{
		string result = null;
		await TestServices.RunOnUIThread(() =>
		{
			var sb = new StringBuilder();
			sb.AppendLine($"--- {label} ---");

			// WindowHelper.XamlRoot (what PressKeySequence uses)
			var whXamlRoot = TestServices.WindowHelper.XamlRoot;
			sb.AppendLine($"WindowHelper.XamlRoot: {Describe(whXamlRoot)}");

			// Expected element state
			if (expectedFocusedElement is not null)
			{
				sb.AppendLine($"ExpectedFocused.XamlRoot: {Describe(expectedFocusedElement.XamlRoot)}");
				if (expectedFocusedElement is Microsoft.UI.Xaml.Controls.Control asControl)
				{
					sb.AppendLine($"ExpectedFocused.FocusState: {asControl.FocusState}");
				}
				sb.AppendLine($"ExpectedFocused.IsLoaded: {(expectedFocusedElement as FrameworkElement)?.IsLoaded}");
				sb.AppendLine($"ExpectedFocused.Visibility: {expectedFocusedElement.Visibility}");
				sb.AppendLine($"ExpectedFocused.Opacity: {expectedFocusedElement.Opacity}");
				sb.AppendLine($"ExpectedFocused == WindowHelper.XamlRoot.Content ancestor: {IsInTree(expectedFocusedElement, whXamlRoot)}");
			}

			// Focus according to WindowHelper.XamlRoot (the one KeyboardHelper queries)
			try
			{
				var focusedByWh = whXamlRoot is null ? null : FocusManager.GetFocusedElement(whXamlRoot);
				sb.AppendLine($"Focused (WindowHelper.XamlRoot): {Describe(focusedByWh)}");
				sb.AppendLine($"Focused equals expected: {ReferenceEquals(focusedByWh, expectedFocusedElement)}");
			}
			catch (Exception ex)
			{
				sb.AppendLine($"FocusManager.GetFocusedElement(WindowHelper.XamlRoot) threw: {ex.GetType().Name}: {ex.Message}");
			}

			// Focus according to element's own XamlRoot
			try
			{
				var focusedByElement = expectedFocusedElement?.XamlRoot is null
					? null
					: FocusManager.GetFocusedElement(expectedFocusedElement.XamlRoot);
				sb.AppendLine($"Focused (element.XamlRoot): {Describe(focusedByElement)}");
			}
			catch (Exception ex)
			{
				sb.AppendLine($"FocusManager.GetFocusedElement(element.XamlRoot) threw: {ex.GetType().Name}: {ex.Message}");
			}

			// Keyboard state tracker
			sb.AppendLine("KeyboardStateTracker states:");
			foreach (var k in _trackedKeys)
			{
				var state = KeyboardStateTracker.GetKeyState(k);
				if (state != CoreVirtualKeyStates.None)
				{
					sb.AppendLine($"  {k} = {state}");
				}
			}

			// Live accelerators
			try
			{
				var contentRoot = expectedFocusedElement is null
					? null
					: VisualTree.GetContentRootForElement(expectedFocusedElement);
				if (contentRoot is not null)
				{
					var list = contentRoot.GetAllLiveKeyboardAccelerators();
					sb.AppendLine($"ContentRoot live KA collection count: {list.Count}");
					int alive = 0, dead = 0, containsExpected = 0;
					foreach (var entry in list)
					{
						if (entry.KeyboardAcceleratorCollectionWeak.IsAlive)
						{
							alive++;
							var coll = entry.KeyboardAcceleratorCollectionWeak.Target as KeyboardAcceleratorCollection;
							if (coll is not null && expectedAccelerator is not null)
							{
								foreach (KeyboardAccelerator ka in coll)
								{
									if (ReferenceEquals(ka, expectedAccelerator))
									{
										containsExpected++;
										break;
									}
								}
							}
						}
						else
						{
							dead++;
						}
					}
					sb.AppendLine($"  alive={alive} dead(weak)={dead} containsExpectedAccelerator={containsExpected}");
				}
				else
				{
					sb.AppendLine("ContentRoot is null (element not in visual tree or no XamlRoot)");
				}
			}
			catch (Exception ex)
			{
				sb.AppendLine($"Live KA enumeration threw: {ex.GetType().Name}: {ex.Message}");
			}

			// Expected accelerator properties
			if (expectedAccelerator is not null)
			{
				sb.AppendLine(
					$"ExpectedAccelerator: Key={expectedAccelerator.Key}, Modifiers={expectedAccelerator.Modifiers}," +
					$" IsEnabled={expectedAccelerator.IsEnabled}," +
					$" ScopeOwner={Describe(expectedAccelerator.ScopeOwner)}");
			}

			result = sb.ToString();
		});

		return result;
	}

	private static bool IsInTree(UIElement element, XamlRoot xamlRoot)
	{
		if (element is null || xamlRoot is null)
		{
			return false;
		}

		var content = xamlRoot.Content;
		if (content is null)
		{
			return false;
		}

		DependencyObject current = element;
		while (current is not null)
		{
			if (ReferenceEquals(current, content))
			{
				return true;
			}
			current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
		}
		return false;
	}

	private static string Describe(object o)
	{
		if (o is null)
		{
			return "<null>";
		}
		if (o is FrameworkElement fe)
		{
			var name = string.IsNullOrEmpty(fe.Name) ? "" : $" Name='{fe.Name}'";
			return $"{fe.GetType().Name}{name} (hash={fe.GetHashCode():X})";
		}
		return $"{o.GetType().Name} (hash={o.GetHashCode():X})";
	}
}
#endif
