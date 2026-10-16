#nullable enable

using System;
using System.Collections.Generic;
using Windows.UI.Input.Preview.Injection;

namespace Uno.UI.Runtime.Wayland.Input;

// UI thread. Managed focus wins without compositor activation; otherwise use the last activation.
internal sealed class WaylandKeyboardInjectionPolicy : IInputInjectorKeyboardTargetPolicy
{
	private WeakReference<IInputInjectorTarget>? _focused;
	private WeakReference<IInputInjectorTarget>? _nativeActive;
	private readonly List<WeakReference<IInputInjectorTarget>> _activated = new();

	internal void Focused(IInputInjectorTarget target) => _focused = new(target);

	internal void Unfocused(IInputInjectorTarget target)
	{
		if (_focused is not null && _focused.TryGetTarget(out var previous) && previous == target)
		{
			_focused = null;
		}
	}

	internal void NativeActivationChanged(IInputInjectorTarget target, bool active)
	{
		if (active)
		{
			_nativeActive = new(target);
		}
		else if (_nativeActive is not null && _nativeActive.TryGetTarget(out var previous) && previous == target)
		{
			_nativeActive = null;
		}
	}

	public bool IsActive(IInputInjectorTarget target)
		=> _nativeActive is not null && _nativeActive.TryGetTarget(out var active) && active == target;

	internal void Activated(IInputInjectorTarget target)
	{
		for (var i = _activated.Count - 1; i >= 0; i--)
		{
			if (!_activated[i].TryGetTarget(out var previous) || previous == target)
			{
				_activated.RemoveAt(i);
			}
		}
		_activated.Add(new(target));
	}

	internal void Remove(IInputInjectorTarget target)
	{
		Unfocused(target);
		NativeActivationChanged(target, active: false);
		for (var i = _activated.Count - 1; i >= 0; i--)
		{
			if (!_activated[i].TryGetTarget(out var previous) || previous == target)
			{
				_activated.RemoveAt(i);
			}
		}
	}

	public IInputInjectorTarget? GetFallbackTarget()
	{
		if (GetLiveTarget(_focused) is { } focused)
		{
			return focused;
		}
		for (var i = _activated.Count - 1; i >= 0; i--)
		{
			if (!_activated[i].TryGetTarget(out _))
			{
				_activated.RemoveAt(i);
			}
			else if (GetLiveTarget(_activated[i]) is { } activated)
			{
				return activated;
			}
		}
		return null;
	}

	private static IInputInjectorTarget? GetLiveTarget(WeakReference<IInputInjectorTarget>? reference)
		=> reference is not null && reference.TryGetTarget(out var target)
			&& target.IsReadyForKeyboardInput && target.IsVisibleForKeyboardInput ? target : null;
}
