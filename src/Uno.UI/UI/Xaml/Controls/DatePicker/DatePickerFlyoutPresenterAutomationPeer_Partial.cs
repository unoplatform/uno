using Windows.UI.Xaml.Controls;

namespace Windows.UI.Xaml.Automation.Peers
{
	public partial class DatePickerFlyoutPresenterAutomationPeer
	{
#if false
		private const string UIA_AP_DATEPICKER_NAME = "datepicker";

		void InitializeImpl(DatePickerFlyoutPresenter pOwner)
		{

			//wrl.ComPtr<xaml.Automation.Peers.FrameworkElementAutomationPeerFactory> spInnerFactory;
			//wrl.ComPtr<xaml.Automation.Peers.FrameworkElementAutomationPeer> spInnerInstance;
			//wrl.ComPtr<xaml.FrameworkElement> spDatePickerFlyoutPresenterAsFE;
			//wrl.ComPtr<xaml_controls.IDatePickerFlyoutPresenter> spOwner(pOwner);
			//wrl.ComPtr<DependencyObject> spInnerInspectable;

			//ARG_NOTnull(pOwner, "pOwner");

			//DatePickerFlyoutPresenterAutomationPeerGenerated.InitializeImpl(pOwner);
			//(wf.GetActivationFactory(
			//      wrl_wrappers.Hstring(RuntimeClass_Microsoft_UI_Xaml_Automation_Peers_FrameworkElementAutomationPeer),
			//      &spInnerFactory));

			//(((DependencyObject)(pOwner)).QueryInterface<xaml.FrameworkElement>(
			//    &spDatePickerFlyoutPresenterAsFE));

			//(spInnerFactory.CreateInstanceWithOwner(
			//        spDatePickerFlyoutPresenterAsFE,
			//        (xaml_automation_peers.IDatePickerFlyoutPresenterAutomationPeer)(this),
			//        &spInnerInspectable,
			//        &spInnerInstance));

			//(SetComposableBasePointers(
			//        spInnerInspectable,
			//        spInnerFactory));
		}

		#region IAutomationPeerOverrides

		AutomationControlType GetAutomationControlTypeCoreImpl()
		{
			return Automation.Peers.AutomationControlType.Pane;
		}

		string GetClassNameCoreImpl()
		{
			return "DatePickerFlyoutPresenter";
		}

		string GetNameCoreImpl()
		{
			return UIA_AP_DATEPICKER_NAME;
		}
		#endregion
#endif
	}
}
