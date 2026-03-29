using System.Collections.Generic;
using DirectUI;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class PasswordBoxAutomationPeer : FrameworkElementAutomationPeer
	{
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
