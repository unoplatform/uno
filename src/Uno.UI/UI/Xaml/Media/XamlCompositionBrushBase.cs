namespace Windows.UI.Xaml.Media
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
		
		protected virtual void OnConnected()
		{
		}

		protected virtual void OnDisconnected()
		{
		}		
	}
}
