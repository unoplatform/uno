#nullable enable

using System;
using Windows.UI.Xaml.Media;

namespace Uno.UI.Helpers;

internal class WeakBrushChangedProxy : WeakEventProxy<Brush, Action>
{
	private WeakReference<Action>? _additionalDisposeAction;

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

	public void Subscribe(Brush? source, Action handler, Action? additionalDisposeAction)
	{
		if (TryGetSource(out var s))
		{
			s.InvalidateRender -= OnBrushChanged;
			if (_additionalDisposeAction is not null && _additionalDisposeAction.TryGetTarget(out var additionalAction))
			{
				additionalAction();
				_additionalDisposeAction = null;
			}
		}

		handler();
		_additionalDisposeAction = additionalDisposeAction is null ? null : new WeakReference<Action>(additionalDisposeAction);

		if (source is not null)
		{
			source.InvalidateRender += OnBrushChanged;
			base.Subscribe(source, handler);
		}
	}

	public override void Subscribe(Brush? source, Action handler)
		=> Subscribe(source, handler, null);

	public override void Unsubscribe()
	{
		if (TryGetSource(out var s))
		{
			s.InvalidateRender -= OnBrushChanged;
			if (_additionalDisposeAction is not null && _additionalDisposeAction.TryGetTarget(out var additionalAction))
			{
				additionalAction();
				_additionalDisposeAction = null;
			}
		}

		base.Unsubscribe();
	}
}
