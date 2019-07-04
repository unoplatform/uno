using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using Uno.UI.Common;
using Windows.UI.Core;

namespace Windows.UI.Xaml
{
	/// <summary>
	/// A <see cref="ConditionalDisposable"/> class that executes the dispose action on the Dispatcher.
	/// </summary>
	/// <remarks>
	/// This class is generally used to ensure that a delegate instance can be tied to its
	/// target property instance, while being a weak reference.
	///
	/// This enables scenarios such as passing <see cref="DependencyObject.RegisterPropertyChangedCallback(DependencyProperty, DependencyPropertyChangedCallback)"/> a
	/// capturing lambda to be passed as a callback, and not have to unintended memory leaks
	/// on either the sender or receiver of the callback.
	/// </remarks>
    internal class DispatcherConditionalDisposable : ConditionalDisposable
	{
		private readonly WeakReference _conditionSource;

		public DispatcherConditionalDisposable(object target, WeakReference conditionSource, Action action) : base(target, Wrap(action), conditionSource)
		{
			_conditionSource = conditionSource;
		}

		private static Action Wrap(Action action)
		{
			return () => 
			{
				if (CoreDispatcher.Main.HasThreadAccess)
				{
					action();
				}
				else
				{
					CoreDispatcher.Main.RunIdleAsync(
						delegate
						{
							action();
						}
					);
				}
			};
		}
	}
}
