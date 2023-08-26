using UIKit;

namespace Windows.UI.Xaml.Controls.Primitives;

public partial class Popup
{
	private UIView _mainWindow;

	public UIView MainWindow
	{
		get
		{
			if (_mainWindow == null)
			{
				_mainWindow = UIApplication.SharedApplication.KeyWindow ?? UIApplication.SharedApplication.Windows[0];
			}

			return _mainWindow;
		}
	}

	private protected override void OnLoaded()
	{
		base.OnLoaded();

		RegisterPopupPanel();
		RegisterPopupPanelChild();
	}

	private void RegisterPopupPanel()
	{
		if (PopupPanel == null)
		{
			PopupPanel = new PopupPanel(this);
		}

		if (PopupPanel.Superview == null)
		{
			PopupPanel.IsVisualTreeRoot = true;
			MainWindow.AddSubview(PopupPanel);
		}
	}

	private void RegisterPopupPanelChild(bool force = false)
	{
		if ((IsLoaded || force) && Child != null)
		{
			RegisterPopupPanel();

			if (!PopupPanel.Children.Contains(Child))
			{
				PopupPanel.Children.Add(Child);
			}
		}
	}

	private void UnregisterPopupPanelChild(UIElement child = null)
	{
		// If the popup is closed immediately after opening,
		// it might not have time to load and the PopupPanel
		// could be null.
		PopupPanel?.Children.Remove(child ?? Child);
	}

	partial void OnUnloadedPartial()
	{
		PopupPanel?.RemoveFromSuperview();
		UnregisterPopupPanelChild();
	}
}
