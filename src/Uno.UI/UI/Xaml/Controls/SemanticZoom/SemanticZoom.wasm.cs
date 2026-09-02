using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Input;

namespace Microsoft.UI.Xaml.Controls;

public partial class SemanticZoom
{
	protected override void OnApplyTemplate() => base.OnApplyTemplate();

	protected override AutomationPeer OnCreateAutomationPeer() => base.OnCreateAutomationPeer();

	protected override void OnKeyDown(KeyRoutedEventArgs args) => base.OnKeyDown(args);

	protected override void OnPointerWheelChanged(PointerRoutedEventArgs args) => base.OnPointerWheelChanged(args);

	protected override void OnPointerMoved(PointerRoutedEventArgs args) => base.OnPointerMoved(args);
}
