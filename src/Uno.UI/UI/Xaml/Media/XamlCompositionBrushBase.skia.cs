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
			var compositionBrush = CompositionBrush;
			if (compositionBrush is null)
			{
				this.OnConnectedInternal();
			}

			_compositionBrush = new CompositionBrushWrapper(compositionBrush ?? compositor.CreateColorBrush(FallbackColorWithOpacity), compositor);
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
