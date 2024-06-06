using System;
using Microsoft.UI.Xaml;
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
	}

	public event EventHandler ParentChanging;

	public event EventHandler ParentLoaded;

	public event EventHandler ParentUnloaded;

	private void OnParentChanged(object instance, object key, DependencyObjectParentChangedEventArgs args)
	{
		ParentChanging?.Invoke(this, EventArgs.Empty);
		// Unsubscribe from the previous parent
		_registrations.Dispose();

		if (args.NewParent is DependencyObject newParent &&
			newParent.FindFirstParent<FrameworkElement>() is FrameworkElement nearestFeParent)
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

	private void OnParentUnloaded(object sender, RoutedEventArgs e) => ParentLoaded?.Invoke(this, EventArgs.Empty);

	private void OnParentLoaded(object sender, RoutedEventArgs e) => ParentUnloaded?.Invoke(this, EventArgs.Empty);
}
