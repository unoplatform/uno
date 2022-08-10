using Windows.UI.Xaml.Automation.Provider;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class ButtonAutomationPeer : ButtonBaseAutomationPeer, IInvokeProvider
	{
		public ButtonAutomationPeer(Button owner) : base(owner)
		{
		}

		protected override object GetPatternCore(PatternInterface patternInterface)
		{
			if (patternInterface == PatternInterface.Invoke)
			{
				return this;
			}

			return base.GetPatternCore(patternInterface);
		}

		protected override string GetClassNameCore() => nameof(Button);

		protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

		public void Invoke()
		{
			if (IsEnabled())
			{
				(Owner as ButtonBase).ProgrammaticClick();
			}
		}
	}
}
