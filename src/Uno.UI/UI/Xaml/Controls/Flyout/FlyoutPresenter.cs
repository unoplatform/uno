using Windows.UI.Xaml.Input;

namespace Windows.UI.Xaml.Controls
{
	public partial class FlyoutPresenter : ContentControl
	{
		public FlyoutPresenter()
		{
		}

		protected override bool CanCreateTemplateWithoutParent { get; } = true;

		internal override bool IsViewHit() => false;
	}
}
