using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Provider;

namespace Microsoft.UI.Xaml.Automation.Peers;

partial class RichEditBoxAutomationPeer : IValueProvider
{
	private TextAdapter m_textPattern;

	protected override IList<AutomationPeer> GetChildrenCore()
	{
#if __SKIA__
		return GetTextObjectChildrenCore();
#else
		return Array.Empty<AutomationPeer>();
#endif
	}

	private object GetManagedPatternProvider(PatternInterface patternInterface)
	{
		if (patternInterface == PatternInterface.Text
			|| patternInterface == PatternInterface.Text2
			|| patternInterface == PatternInterface.TextEdit)
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

}
