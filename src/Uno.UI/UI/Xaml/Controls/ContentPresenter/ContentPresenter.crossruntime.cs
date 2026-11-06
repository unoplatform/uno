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
	private bool _isRemovingContentTemplateRoot;

	/// <remarks>
	/// MUX: CContentPresenter::ApplyTemplate parents a UIElement Content during Measure, so a presenter layout
	/// can't reach never claims it. Uno parents eagerly, but must keep that outcome: a template can bind the same
	/// element to several presenters and show only one of them, as BreadcrumbBarItem does.
	/// </remarks>
	private static bool CanBeMeasured(UIElement element)
		=> element.IsActiveInVisualTree && element.Visibility != Visibility.Collapsed && element.AreAllAncestorsVisible();

	// Uno specific: WinUI fails if the element is still parented elsewhere, while UIElement.AddChild moves it,
	// which two presenters being laid out would keep doing to each other.
	private void PresentDeferredContent()
	{
		if (!_isRemovingContentTemplateRoot
			&& !HasTemplateChild()
			&& Content is UIElement content
			&& this.ResolveContentTemplate() is null
			&& (content.GetParent() is not UIElement parent || !CanBeMeasured(parent)))
		{
			ContentTemplateRoot = content;
		}
	}

	partial void RegisterContentTemplateRoot() => AddTemplateChild(ContentTemplateRoot);

	partial void UnregisterContentTemplateRoot()
	{
		// Unloaded handlers can run a layout pass while the root is being replaced.
		_isRemovingContentTemplateRoot = true;
		try
		{
			RemoveTemplateChild();
		}
		finally
		{
			_isRemovingContentTemplateRoot = false;
		}
	}

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
