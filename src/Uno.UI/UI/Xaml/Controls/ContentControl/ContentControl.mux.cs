// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// MUX Reference ContentControl.cpp, tag winui3/release/1.8.2, commit bac7a9c33

using System;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;

namespace Microsoft.UI.Xaml.Controls;

partial class ContentControl
{
	private protected override void OnTemplateChanged(DependencyPropertyChangedEventArgs e)
	{
		if (Content is UIElement contentAsUIElement &&
			contentAsUIElement.GetUIElementAdjustedParentInternal() is { } parent)
		{
			parent.RemoveChild(contentAsUIElement);
		}

		base.OnTemplateChanged(e);
	}

	internal static ControlTemplate CreateDefaultTemplate(FrameworkElement parent)
	{
		var template = (ControlTemplate)XamlReader.Load(c_strTextTemplateStorage);
		template.TargetType = parent.GetType();
		return template;
	}

	private static void CreateDefaultVisuals(ContentControl parent, DependencyObject content)
	{
		// If the content is a UIElement then show it.
		if (content is UIElement ui)
		{
			// TODO Uno: WinUI fails with E_INVALIDARG when the element is already associated with another
			// parent and does not allow multiple association. Uno has no IsAssociated /
			// DoesAllowMultipleAssociation equivalent.
			// if (pUI->IsAssociated() && !pUI->DoesAllowMultipleAssociation())
			// {
			//     IFC_RETURN(E_INVALIDARG);
			// }
			parent.AddChild(ui);
		}
		else
		{
			parent.Template = CreateDefaultTemplate(parent);
			parent.ApplyTemplate(out _);
		}
	}

	private void Invalidate(bool clearChildren)
	{
		if (clearChildren && GetChildren() is { Count: > 0 })
		{
			ClearChildren();
			// only clear the suggested cp if we actually removed all children!
			m_pTemplatePresenter = null;
		}

		InvalidateMeasure();
	}

	private protected override void ApplyTemplate(out bool addedVisuals)
	{
		base.ApplyTemplate(out addedVisuals);

		if (!m_bInOnApplyTemplate)
		{
			m_bInOnApplyTemplate = true;

			if (GetFirstChildNoAddRef() is null && Content is { } content)
			{
				CreateDefaultVisuals(this, content as DependencyObject);
				addedVisuals = GetFirstChildNoAddRef() is not null;
			}
		}

		m_bInOnApplyTemplate = false;
	}

	// gets called when a contentpresenter in the template of a contentcontrol is used. It will call back
	// offering its content to compare to the cc's content. If they are the same, we consider that
	// contentpresenter our templateroot.
	internal void ConsiderContentPresenterForContentTemplateRoot(ContentPresenter candidate, object value)
	{
		if (Content == value)
		{
			m_pTemplatePresenter = new WeakReference<ContentPresenter>(candidate);
		}
	}

	/// <summary>
	/// Gets the root of the materialized <see cref="ContentTemplate"/>.
	/// </summary>
	public UIElement ContentTemplateRoot
	{
		get
		{
			UIElement templateRoot = null;

			if (m_pTemplatePresenter?.TryGetTarget(out var pTemplatePresenter) == true)
			{
				// The template child, not the first child: starting with the Cobalt release, when the inner
				// content presenter is a ListViewBaseItemChrome the template root may be the second child,
				// because of the potential backplate positioned in the first slot.
				templateRoot = pTemplatePresenter.GetFirstChildNoAddRef();
			}

			return templateRoot;
		}
	}

	internal override string GetPlainText()
	{
		var content = Content;

		if (content is not null)
		{
			return FrameworkElement.GetStringFromObject(content);
		}

		return null;
	}
}
