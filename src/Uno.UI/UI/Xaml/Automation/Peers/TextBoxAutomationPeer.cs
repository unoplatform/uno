using System.Collections.Generic;
using Microsoft.UI.Xaml.Controls;

namespace Microsoft.UI.Xaml.Automation.Peers
{
	public partial class TextBoxAutomationPeer : FrameworkElementAutomationPeer
	{
		public TextBoxAutomationPeer(TextBox owner) : base(owner)
		{
		}

		protected override string GetClassNameCore()
		{
			return "RichEditBox";
		}

		protected override AutomationControlType GetAutomationControlTypeCore()
		{
			return AutomationControlType.Edit;
		}

		protected override bool IsControlElementCore()
		{
			return true;
		}

		// TODO: Add support for PlaceholderText and Label
		// Accessibility priority: Name > LabeledBy > Header > PlaceholderText
		// LabeledBy: Header, DescribedBy: PlaceholderText
	}
}
