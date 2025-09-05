#nullable enable

using System;
using Microsoft.UI.Xaml;
using Uno.Disposables;
using Uno.UI.Extensions;

namespace Uno.UI.Xaml;

internal class ParentVisualTreeListener
{
	private readonly DependencyObject _owner;
	private readonly Action _onLoaded;
	private readonly Action _onUnloaded;
	private readonly SerialDisposable _registrations = new();

	public ParentVisualTreeListener(DependencyObject owner, Action onLoaded, Action onUnloaded)
	{
		_owner = owner;
		_onLoaded = onLoaded;
		_onUnloaded = onUnloaded;
		owner.RegisterParentChangedCallbackStrong(null, OnParentChanged);

		if (_owner.GetParent() is { } existingParent)
		{
			OnParentChanged(existingParent);
		}
	}

	private void OnParentChanged(object instance, object? key, DependencyObjectParentChangedEventArgs args) => OnParentChanged(args.NewParent);

	private void OnParentChanged(object? newParent)
	{
		// Unsubscribe from the previous parent
		_registrations.Disposable = null;

		if (_owner.FindFirstParent<FrameworkElement>() is FrameworkElement nearestFeParent)
		{
			nearestFeParent.Loaded += OnParentLoaded;
			nearestFeParent.Unloaded += OnParentUnloaded;

			if (nearestFeParent.IsLoaded)
			{
				_onLoaded?.Invoke();
			}

			_registrations.Disposable = Disposable.Create(() =>
			{
				nearestFeParent.Loaded -= OnParentLoaded;
				nearestFeParent.Unloaded -= OnParentUnloaded;
			});
		}
	}

	private void OnParentLoaded(object sender, RoutedEventArgs e) => _onLoaded?.Invoke();

	private void OnParentUnloaded(object sender, RoutedEventArgs e) => _onUnloaded?.Invoke();
}
