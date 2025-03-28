#nullable enable

using System;
using Windows.UI.Xaml;
using Uno.Disposables;
using Uno.UI.Extensions;

namespace Uno.UI.Xaml;

internal class ParentVisualTreeListener
{
	private readonly DependencyObject _owner;
	private readonly SerialDisposable _registrations = new();

	public ParentVisualTreeListener(DependencyObject owner)
	{
		_owner = owner;

		owner.RegisterParentChangedCallbackStrong(null, OnParentChanged);

		if (_owner.GetParent() is { } existingParent)
		{
			OnParentChanged(existingParent);
		}
	}

	public event EventHandler? ParentChanging;

	public event EventHandler? ParentLoaded;

	public event EventHandler? ParentUnloaded;

	private void OnParentChanged(object instance, object? key, DependencyObjectParentChangedEventArgs args) => OnParentChanged(args.NewParent);

	private void OnParentChanged(object? newParent)
	{
		ParentChanging?.Invoke(this, EventArgs.Empty);
		// Unsubscribe from the previous parent
		_registrations.Disposable = null;

		if (_owner.FindFirstParent<FrameworkElement>() is FrameworkElement nearestFeParent)
		{
			nearestFeParent.Loaded += OnParentLoaded;
			nearestFeParent.Unloaded += OnParentUnloaded;

			if (nearestFeParent.IsLoaded)
			{
				ParentLoaded?.Invoke(this, EventArgs.Empty);
			}

			_registrations.Disposable = Disposable.Create(() =>
			{
				nearestFeParent.Loaded -= OnParentLoaded;
				nearestFeParent.Unloaded -= OnParentUnloaded;
			});
		}
	}

	private void OnParentLoaded(object sender, RoutedEventArgs e) => ParentLoaded?.Invoke(this, EventArgs.Empty);

	private void OnParentUnloaded(object sender, RoutedEventArgs e) => ParentUnloaded?.Invoke(this, EventArgs.Empty);
}
