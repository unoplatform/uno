#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentPresenter
{
	partial void RegisterContentTemplateRoot() => AddTemplateChild(ContentTemplateRoot);

	partial void UnregisterContentTemplateRoot() => RemoveTemplateChild();

	// MUX Reference ContentPresenter.cpp, tag winui3/release/2.5.1
	// Uno-specific: WinUI returns the first child; Uno returns the template root so subclasses can keep extra children before it.
	private protected virtual UIElement? GetTemplateChildNoRef() => ContentTemplateRoot;

	private protected virtual void AddTemplateChild(UIElement child) => AddChild(child);

	// Uno-specific: WinUI clears all children (ContentPresenter.cpp:1141), Uno removes only the template root so chrome children survive.
	// TODO Uno: FxCallbacks::ContentPresenter_OnChildrenCleared is not ported.
	private protected virtual void RemoveTemplateChild()
	{
		if (ContentTemplateRoot is { } root)
		{
			RemoveChild(root);
		}
	}
}
