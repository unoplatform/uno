#pragma warning disable 108 // new keyword hiding
#pragma warning disable 114 // new keyword hiding
namespace Microsoft.UI.Xaml.Controls
{
	public partial class MenuBarItemFlyout : MenuFlyout
	{
		internal Control m_presenter;

		public MenuBarItemFlyout() : base()
		{
		}

		protected override Control CreatePresenter()
		{
			m_presenter = base.CreatePresenter();
			return m_presenter;
		}
	}
}
