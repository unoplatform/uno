using Windows.Foundation;

namespace Windows.UI.Xaml.Controls.Primitives
{
    public partial class CommandBarTemplateSettings : DependencyObject
	{
		private readonly CommandBar _commandBar;

		public CommandBarTemplateSettings(CommandBar commandBar)
		{
			_commandBar = commandBar;
		}

		// TODO: Implement
		public double ContentHeight => 0;
        public double NegativeOverflowContentHeight => 0;
		public Rect OverflowContentClipRect => Rect.Empty;
        public double OverflowContentHeight => 0;
		public double OverflowContentHorizontalOffset => 0;
		public double OverflowContentMaxHeight => 0;
        public double OverflowContentMinWidth => 0;
        public double OverflowContentMaxWidth => 0;
		public Visibility EffectiveOverflowButtonVisibility => Visibility.Collapsed;
    }
}
