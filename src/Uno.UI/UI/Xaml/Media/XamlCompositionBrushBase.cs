using Microsoft.UI.Composition;
using Color = Windows.UI.Color;
using System.Runtime.CompilerServices;

namespace Microsoft.UI.Xaml.Media;

public partial class XamlCompositionBrushBase : Brush
{
	protected XamlCompositionBrushBase() : base()
	{
	}

	public Color FallbackColor
	{
		get => (Color)GetValue(FallbackColorProperty);
		set => SetValue(FallbackColorProperty, value);
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

	protected CompositionBrush CompositionBrush
	{
		get => (CompositionBrush)GetValue(CompositionBrushProperty);
		set => SetValue(CompositionBrushProperty, value);
	}

	/// <summary>
	/// Internal DependencyProperty used to track the CompositionBrush property.
	/// </summary>
	internal static DependencyProperty CompositionBrushProperty { get; } =
		DependencyProperty.Register(
			nameof(CompositionBrush), typeof(CompositionBrush),
			typeof(XamlCompositionBrushBase),
			new FrameworkPropertyMetadata(default(CompositionBrush)));

	protected virtual void OnConnected()
	{
	}

	protected virtual void OnDisconnected()
	{
	}

	internal void OnConnectedInternal() => OnConnected();
	internal void OnDisconnectedInternal() => OnDisconnected();

	internal override CompositionBrush GetOrCreateCompositionBrush(Compositor compositor)
	{
		if (_compositionBrush is null)
		{
			if (CompositionBrush is null)
			{
				this.OnConnectedInternal();
			}

			// Don't store CompositionBrush in a local variable. It has to be read again after the null check as OnConnectedInternal may set it.
			// NOTE: We create a CompositionBrushWrapper here because the callers of GetOrCreateCompositionBrush assumes that this method will return
			// the same instance every time. Whenever CompositionBrush changes, we will update CompositionBrushWrapper.WrappedBrush.
			_compositionBrush = new CompositionBrushWrapper(CompositionBrush ?? compositor.CreateColorBrush(FallbackColorWithOpacity), compositor);
			SynchronizeCompositionBrush();
		}

		return _compositionBrush;
	}

	internal override void OnPropertyChanged2(DependencyPropertyChangedEventArgs args)
	{
		base.OnPropertyChanged2(args);

		if (args.Property == CompositionBrushProperty && _compositionBrush is CompositionBrushWrapper wrapper)
		{
			wrapper.WrappedBrush = (args.NewValue as CompositionBrush) ?? wrapper.Compositor.CreateColorBrush(FallbackColorWithOpacity);
		}
	}

	internal override void SynchronizeCompositionBrush()
	{
		base.SynchronizeCompositionBrush();

		_compositionBrush.TrySetColorFromBrush(this);
	}
}
