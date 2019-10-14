using Uno.Extensions;
using Uno.Logging;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	partial class ToolTip // That's a ContentControl
	{
		private readonly Popup _popup = new Popup {IsLightDismissEnabled = false};

		internal Popup Popup => _popup;

		public ToolTip()
		{
			DefaultStyleKey = typeof(ToolTip);

			_popup.PopupPanel = new ToolTipPopupPanel(this);
			_popup.Closed += (sender, e) => IsOpen = false;
		}

		public static DependencyProperty PlacementProperty { get; } =
			DependencyProperty.Register(
				"Placement", typeof(PlacementMode),
				typeof(ToolTip),
				new FrameworkPropertyMetadata(PlacementMode.Top));

		public PlacementMode Placement
		{
			get => (PlacementMode)GetValue(PlacementProperty);
			set => SetValue(PlacementProperty, value);
		}

		public static DependencyProperty IsOpenProperty { get; } =
			DependencyProperty.Register(
				"IsOpen", typeof(bool),
				typeof(ToolTip),
				new FrameworkPropertyMetadata(default(bool), OnOpenChanged));

		public bool IsOpen
		{
			get => (bool)GetValue(IsOpenProperty);
			set => SetValue(IsOpenProperty, value);
		}

		private static void OnOpenChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args) => (sender as ToolTip)?.OnOpenChanged((bool)args.NewValue);

		private void OnOpenChanged(bool isOpen)
		{
			_popup.IsOpen = isOpen;
			if (isOpen)
			{
				AttachToPopup();

				Opened?.Invoke(this, new RoutedEventArgs(this));
				GoToElementState("Opened", useTransitions: true);
			}
			else
			{
				Closed?.Invoke(this, new RoutedEventArgs(this));
				GoToElementState("Closed", useTransitions: true);
			}
		}

		private void AttachToPopup()
		{
			if (Parent == null)
			{
				_popup.Child = this;
			}
			else if (!ReferenceEquals(Parent, _popup))
			{
				this.Log().Warn($"This ToolTip is already in visual tree: won't be able to use it with TooltipService.");
			}
		}

		public void SetAnchor(UIElement element) => _popup.Anchor = element;

		public event RoutedEventHandler Closed;

		public event RoutedEventHandler Opened;
	}
}
