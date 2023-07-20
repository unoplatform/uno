#nullable enable

using System;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Helpers;

internal class WeakBrushChangedProxy : WeakEventProxy<Brush, Action>
{
	private void OnBrushChanged()
	{
		if (TryGetHandler(out var handler))
		{
			handler();
		}
		else
		{
			Unsubscribe();
		}
	}

	public override void Subscribe(Brush? source, Action handler)
	{
		if (TryGetSource(out var s))
		{
			s.InvalidateRender -= OnBrushChanged;
		}

		handler();

		if (source is not null)
		{
			source.InvalidateRender += OnBrushChanged;
			base.Subscribe(source, handler);
		}
	}

	public override void Unsubscribe()
	{
		if (TryGetSource(out var s))
		{
			s.InvalidateRender -= OnBrushChanged;
		}

		base.Unsubscribe();
	}
}
