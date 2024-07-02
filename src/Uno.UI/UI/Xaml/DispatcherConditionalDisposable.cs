using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using Uno.UI.Common;
using Windows.UI.Core;
using Uno.UI.Dispatching;

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
	internal abstract class DispatcherConditionalDisposable : ConditionalDisposable
	{
		public DispatcherConditionalDisposable(object target, WeakReference conditionSource) : base(target, conditionSource)
		{
		}

		protected override void TargetFinalized()
		{
			if (CoreDispatcher.Main.HasThreadAccess
#if __WASM__
				|| !NativeDispatcher.IsThreadingSupported
#endif
				)
			{
				DispatchedTargetFinalized();
			}
			else
			{
				Uno.UI.Dispatching.NativeDispatcher.Main.Enqueue(
					DispatchedTargetFinalized,
					Uno.UI.Dispatching.NativeDispatcherPriority.Idle);
			}
		}

		protected abstract void DispatchedTargetFinalized();
	}
}
