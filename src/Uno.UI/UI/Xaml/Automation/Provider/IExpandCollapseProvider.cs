namespace Microsoft.UI.Xaml.Automation.Provider
{
	public partial interface IExpandCollapseProvider
	{
		ExpandCollapseState ExpandCollapseState { get; }

		void Collapse();

		void Expand();
	}
}
