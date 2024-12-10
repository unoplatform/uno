using System.Runtime.CompilerServices;
using Windows.UI.Composition;
using Color = Windows.UI.Color;

namespace Windows.UI.Xaml.Media;

public partial class XamlCompositionBrushBase : Brush
{
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
