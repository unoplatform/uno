// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference InlineUIContainer.cpp, tag winui3/release/1.8.2, commit 4a1c6184c

#nullable enable

namespace Microsoft.UI.Xaml.Documents;

partial class InlineUIContainer
{
	// CInlineUIContainer::m_pChild, the storage behind the Child property.
	private UIElement? m_pChild;

	/// <summary>
	/// Gets or sets the UI element that is contained within the <see cref="InlineUIContainer"/>.
	/// </summary>
	public UIElement Child
	{
		get => (UIElement)GetValue(ChildProperty);
		set => SetValue(ChildProperty, value);
	}

	public static DependencyProperty ChildProperty { get; } =
		DependencyProperty.Register(
			nameof(Child),
			typeof(UIElement),
			typeof(InlineUIContainer),
			new FrameworkPropertyMetadata(
				defaultValue: null,
				propertyChangedCallback: (s, e) => ((InlineUIContainer)s).OnChildChanged(e.NewValue as UIElement)
			)
		);

	// Attaching to / detaching from the embedded element host is block-layout (Skia) only.
	partial void DetachChildFromHost();

	partial void AttachChildToCachedHost();

	//------------------------------------------------------------------------
	//  Summary:
	//      SetValue override to update the embedded element host when a new child is set.
	//------------------------------------------------------------------------
	private void OnChildChanged(UIElement? newChild)
	{
		// This is a three-step process:
		//      1. Remove the current child from the host.
		//      2. Update the current child.
		//      2. Add the new child, if any, to the host.
		if (m_pChild is not null)
		{
			RemoveLogicalChild(m_pChild);
			DetachChildFromHost();
		}

		m_pChild = newChild;

		if (m_pChild is not null)
		{
			// WinUI additionally sets a peer reference to the property value here, because once the
			// child is reparented to the host it would otherwise be collectable. The DependencyProperty
			// value is a strong reference, so there is nothing to do.
			AttachChildToCachedHost();
			AddLogicalChild(m_pChild);
		}

		// WinUI marks Child as AffectsMeasure; a TextElement is not a FrameworkElement, so the owning
		// text control is invalidated through the inline tree instead.
		InvalidateInlines(updateText: false);
	}

	// This is duplicated code from CFrameworkElement::AddLogicalChild as CInlineUIContainer is a
	// DependencyObject and cannot use the same logic in CFrameworkElement::AddLogicalChild and
	// requires a more restricted form of it.
	private void AddLogicalChild(DependencyObject pNewLogicalChild)
	{
		if (pNewLogicalChild is FrameworkElement pChild && pChild.LogicalParentOverride != this)
		{
			//if logical parent is already set, then content should be template bound. otherwise, this is an error.
			if (pChild.LogicalParentOverride is null)
			{
				pChild.LogicalParentOverride = this;
			}
		}
	}

	// This is duplicated code from CFrameworkElement::RemoveLogicalChild as CInlineUIContainer is a
	// DependencyObject and cannot use the same logic in CFrameworkElement::RemoveLogicalChild and
	// requires a more restricted form of it.
	private void RemoveLogicalChild(DependencyObject pOldLogicalChild)
	{
		if (pOldLogicalChild is FrameworkElement pChild && pChild.LogicalParentOverride == this)
		{
			//set it back to the default value
			pChild.LogicalParentOverride = null;
		}
	}
}
