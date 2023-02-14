using Color = Windows.UI.Color;

namespace Microsoft.UI.Xaml.Media
{
	public partial class XamlCompositionBrushBase : Brush
	{
		protected XamlCompositionBrushBase() : base()
		{
		}

		public Color FallbackColor
		{
			get
			{
				return (Color)this.GetValue(FallbackColorProperty);
			}
			set
			{
				this.SetValue(FallbackColorProperty, value);
			}
		}

		public static DependencyProperty FallbackColorProperty { get; } =
			DependencyProperty.Register(
				nameof(FallbackColor), typeof(Color),
				typeof(XamlCompositionBrushBase),
				new FrameworkPropertyMetadata(default(Color)));

		/// <summary>
		/// Returns the fallback color mixed with opacity value.
		/// </summary>
		internal Color FallbackColorWithOpacity => FallbackColor.WithOpacity(Opacity);

		protected virtual void OnConnected()
		{
		}

		protected virtual void OnDisconnected()
		{
		}
	}
}
