using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls
{
	public partial class SystemFocusVisual : Control
	{
		public SystemFocusVisual()
		{
			DefaultStyleKey = typeof(SystemFocusVisual);
		}

		protected override void OnApplyTemplate() => base.OnApplyTemplate();

		public FrameworkElement FocusedElement
		{
			get => (FrameworkElement)GetValue(FocusedElementProperty);
			set => SetValue(FocusedElementProperty, value);
		}

		public static readonly DependencyProperty FocusedElementProperty =
			DependencyProperty.Register(nameof(FocusedElement), typeof(FrameworkElement), typeof(SystemFocusVisual), new PropertyMetadata(default));
	}
}
