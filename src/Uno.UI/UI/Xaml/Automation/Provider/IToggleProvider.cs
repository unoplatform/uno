namespace Microsoft.UI.Xaml.Automation.Provider
{
	public partial interface IToggleProvider
	{
		ToggleState ToggleState { get; }

		void Toggle();
	}
}
