using System.Runtime.CompilerServices;
using Microsoft.UI.Composition;
using Color = Windows.UI.Color;

namespace Microsoft.UI.Xaml.Media;

public partial class XamlCompositionBrushBase : Brush
{
	private ConditionalWeakTable<BrushSetterHandler, object> _brushSetters = new();

	internal override CompositionBrush GetOrCreateCompositionBrush(Compositor compositor)
	{
		if (_compositionBrush is null)
		{
			if (CompositionBrush is null)
			{
				this.OnConnectedInternal();
			}

			// Don't store CompositionBrush in a local variable. It has to be read again after the null check as OnConnectedInternal may set it.
			_compositionBrush = new CompositionBrushWrapper(CompositionBrush ?? compositor.CreateColorBrush(FallbackColorWithOpacity), compositor);
			SynchronizeCompositionBrush();
		}

		return _compositionBrush;
	}

	internal override void SynchronizeCompositionBrush()
	{
		base.SynchronizeCompositionBrush();

		_compositionBrush.TrySetColorFromBrush(this);
	}
}
