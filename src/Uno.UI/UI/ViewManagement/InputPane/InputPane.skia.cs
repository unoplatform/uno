#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Uno.Foundation.Extensibility;
using Uno.UI.Extensions;

namespace Windows.UI.ViewManagement;

partial class InputPane
{
	private Lazy<IInputPaneExtension?>? _inputPaneExtension;

	partial void InitializePlatform()
	{
		_inputPaneExtension = new(() =>
		{
			ApiExtensibility.CreateInstance<IInputPaneExtension>(this, out var extension);
			return extension;
		});
	}

	private bool TryShowPlatform() => _inputPaneExtension?.Value?.TryShow() ?? false;

	private bool TryHidePlatform() => _inputPaneExtension?.Value?.TryHide() ?? false;

	partial void EnsureFocusedElementInViewPartial()
	{
		if (Visible && FocusManager.GetFocusedElement() is UIElement focusedElement)
		{
			focusedElement.StartBringIntoView();
		}
	}
}
