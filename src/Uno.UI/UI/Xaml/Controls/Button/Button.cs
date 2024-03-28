using Windows.UI.Xaml.Automation.Peers;
using Windows.UI.Xaml.Controls.Primitives;

namespace Windows.UI.Xaml.Controls
{
	public partial class Button : ButtonBase
	{
		static Button()
		{
			HorizontalContentAlignmentProperty.OverrideMetadata(
				typeof(Button),
				new FrameworkPropertyMetadata(HorizontalAlignment.Center)
			);

			VerticalContentAlignmentProperty.OverrideMetadata(
				typeof(Button),
				new FrameworkPropertyMetadata(VerticalAlignment.Center)
			);
		}

		/// <summary>
		/// Initializes a new instance of the Button class.
		/// </summary>
		public Button()
		{
			DefaultStyleKey = typeof(Button);
		}

		/// <summary>
		/// Gets or sets the flyout associated with this button.
		/// </summary>
		public FlyoutBase Flyout
		{
			get => (FlyoutBase)GetValue(FlyoutProperty);
			set => SetValue(FlyoutProperty, value);
		}

		/// <summary>
		/// Identifies the Flyout dependency property.
		/// </summary>
		public static DependencyProperty FlyoutProperty { get; } =
			DependencyProperty.Register(
				nameof(Flyout),
				typeof(FlyoutBase),
				typeof(Button),
				new FrameworkPropertyMetadata(
					defaultValue: null,
					options: FrameworkPropertyMetadataOptions.LogicalChild
				)
			);

		private protected override void OnUnloaded()
		{
			base.OnUnloaded();

			if (Flyout is { IsOpen: true } flyout)
			{
				flyout.Close();
			}
		}
	}
}
