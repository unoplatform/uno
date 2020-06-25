using System;

namespace Windows.UI.Xaml.Controls
{
	public partial class ToggleSwitch
	{
		private void OnHeaderChanged(DependencyPropertyChangedEventArgs e)
		{
			if (GetTemplateChild("HeaderContentPresenter") is ContentPresenter headerContentPresenter)
			{
				headerContentPresenter.Visibility = e.NewValue != null ? Visibility.Visible : Visibility.Collapsed;
			}
		}
	}
}
