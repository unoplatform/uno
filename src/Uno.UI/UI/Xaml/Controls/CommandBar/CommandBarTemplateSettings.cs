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

		public double ContentHeight { get; internal set; }
		public double NegativeOverflowContentHeight { get; internal set; }
		public Rect OverflowContentClipRect { get; internal set; }
		public double OverflowContentHeight { get; internal set; }
		public double OverflowContentHorizontalOffset { get; internal set; }
		public double OverflowContentMaxHeight { get; internal set; }
		public double OverflowContentMinWidth { get; internal set; }
		public double OverflowContentMaxWidth { get; internal set; }
		public Visibility EffectiveOverflowButtonVisibility { get; internal set; }
	}
}
