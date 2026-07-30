// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// MUX Reference RichEditBoxAutomationPeer_Partial.cpp, tag winui3/release/1.8.4
using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers;

/// <summary>
/// Exposes RichEditBox types to Microsoft UI Automation.
/// </summary>
/// <remarks>
/// On Skia, links and inline images are managed text-object children for Text/Text2 range navigation.
/// A browser textarea cannot contain semantic element children, so the WebAssembly semantic DOM keeps
/// RichEditBox as one native textarea and does not duplicate its inline text as descendant link/image
/// nodes. The managed automation tree remains available without adding a Value pattern.
/// </remarks>
public partial class RichEditBoxAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
{
	private TextAdapter m_textPattern;

	public RichEditBoxAutomationPeer(Controls.RichEditBox owner) : base(owner)
	{
	}

	protected override string GetClassNameCore() => nameof(Controls.RichEditBox);

	protected override AutomationControlType GetAutomationControlTypeCore()
		=> AutomationControlType.Edit;

	protected override IList<AutomationPeer> GetChildrenCore()
	{
#if __SKIA__
		return GetTextObjectChildrenCore();
#else
		return Array.Empty<AutomationPeer>();
#endif
	}

	protected override object GetPatternCore(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Text
			|| patternInterface == PatternInterface.Text2)
		{
			if (m_textPattern is null && Owner is Controls.RichEditBox owner)
			{
				m_textPattern = new TextAdapter(owner, this);
			}
			return m_textPattern;
		}

		return base.GetPatternCore(patternInterface);
	}

	internal void RaiseIsReadOnlyPropertyChangedEvent(bool oldValue, bool newValue)
		=> RaisePropertyChangedEvent(ValuePatternIdentifiers.IsReadOnlyProperty, oldValue, newValue);

	/// <inheritdoc />
	public string Value => TextAdapter.GetEffectiveText((Controls.RichEditBox)Owner);

	/// <inheritdoc />
	public bool IsReadOnly => ((Controls.RichEditBox)Owner).IsReadOnly;

	/// <inheritdoc />
	public void SetValue(string value)
	{
		if (IsReadOnly)
		{
			throw new InvalidOperationException("Cannot set value on a read-only RichEditBox.");
		}

		((Controls.RichEditBox)Owner).Document?.SetText(
			Microsoft.UI.Text.TextSetOptions.None,
			value ?? string.Empty);
	}

	protected override string GetNameCore()
	{
		var baseName = base.GetNameCore();
		if (!string.IsNullOrEmpty(baseName))
		{
			return baseName;
		}

		// WinUI3 uses the Header as the accessible name when no Name / LabeledBy is set.
		if (Owner is Controls.RichEditBox { Header: { } header })
		{
			var headerText = header.ToString();
			if (!string.IsNullOrEmpty(headerText))
			{
				return headerText;
			}
		}

		// Fall back to PlaceholderText when no Header is available.
		if (Owner is Controls.RichEditBox { PlaceholderText: { } placeholder } && !string.IsNullOrEmpty(placeholder))
		{
			return placeholder;
		}

		return string.Empty;
	}

	protected override string GetHelpTextCore()
	{
		var baseHelp = base.GetHelpTextCore();
		if (!string.IsNullOrEmpty(baseHelp))
		{
			return baseHelp;
		}

		// When Header provides the name, PlaceholderText serves as help text.
		if (Owner is Controls.RichEditBox { Header: { } header, PlaceholderText: { } placeholder }
			&& !string.IsNullOrEmpty(header.ToString())
			&& !string.IsNullOrEmpty(placeholder))
		{
			return placeholder;
		}

		return string.Empty;
	}

	protected override IEnumerable<AutomationPeer> GetDescribedByCore()
	{
		if (Owner is Controls.RichEditBox owner)
		{
			TextBoxPlaceholderTextHelper.SetupPlaceholderTextBlockDescribedBy(owner);
		}

		return base.GetDescribedByCore();
	}
}
