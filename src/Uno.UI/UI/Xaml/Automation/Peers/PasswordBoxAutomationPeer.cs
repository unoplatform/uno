using System;
using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Automation.Provider;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class PasswordBoxAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
	{
		// Cached TextAdapter that masks the underlying password. Created lazily so
		// password content is never read until UIA explicitly asks for it, and the
		// adapter never sees the raw Password — only a masked snapshot via the
		// owning PasswordBox.
		private TextAdapter m_textPattern;

		public PasswordBoxAutomationPeer(PasswordBox owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "PasswordBox";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Edit;
		}

		protected override bool IsPasswordCore()
		{
			return true;
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Value)
			{
				return this;
			}

			if (patternInterface == PatternInterface.Text
				|| patternInterface == PatternInterface.Text2
				|| patternInterface == PatternInterface.TextEdit)
			{
				if (m_textPattern is null && Owner is PasswordBox owner)
				{
					m_textPattern = new TextAdapter(owner, this);
				}
				return m_textPattern;
			}

			return base.GetPatternCore(patternInterface);
		}

		// IValueProvider — UIA convention is that PasswordBox returns a string of
		// masked characters matching the password length, never the password itself.
		// Screen readers respect IsPassword and announce "protected" without reading
		// the masked characters aloud.
		public string Value
		{
			get
			{
				var password = (Owner as PasswordBox)?.Password ?? string.Empty;
				return password.Length == 0
					? string.Empty
					: new string('•', password.Length);
			}
		}

		public bool IsReadOnly => false;

		public void SetValue(string value)
		{
			if (Owner is PasswordBox passwordBox)
			{
				passwordBox.Password = value ?? string.Empty;
			}
		}

		protected override string GetNameCore()
		{
			var baseName = base.GetNameCore();
			if (!string.IsNullOrEmpty(baseName))
			{
				return baseName;
			}

			// WinUI3 uses the Header as the accessible name for PasswordBox
			if (Owner is PasswordBox { Header: { } header })
			{
				var headerText = header.ToString();
				if (!string.IsNullOrEmpty(headerText))
				{
					return headerText;
				}
			}

			// Fall back to PlaceholderText
			if (Owner is PasswordBox { PlaceholderText: { } placeholder } && !string.IsNullOrEmpty(placeholder))
			{
				return placeholder;
			}

			return string.Empty;
		}

		protected override IEnumerable<AutomationPeer> GetDescribedByCore()
		{
			if (Owner is PasswordBox owner)
			{
				TextBoxPlaceholderTextHelper.SetupPlaceholderTextBlockDescribedBy(owner);
			}

			return base.GetDescribedByCore();
		}
	}
}
