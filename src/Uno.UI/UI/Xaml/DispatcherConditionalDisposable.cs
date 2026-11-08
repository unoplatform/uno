using System;
using System.Collections.Generic;
using System.Text;
using Uno.Disposables;
using Uno.UI.Common;
using Windows.UI.Core;
using Uno.Foundation.Logging;
using Uno.UI.Dispatching;
using Uno.UI.Helpers;

namespace Microsoft.UI.Xaml
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
	internal abstract class DispatcherConditionalDisposable : ConditionalDisposable, IQueuedRemoval
	{
		public DispatcherConditionalDisposable(object target, object conditionSource) : base(target, conditionSource)
		{
		}

		protected override void TargetFinalized()
		{
			if (CoreDispatcher.Main.HasThreadAccess)
			{
				DispatchedTargetFinalized();
			}
			else
			{
				// From the finalizer: batched with the other removals of collected subscribers, rather than one dispatcher item each.
				SubscriptionCleanup.EnqueueFromFinalizer(this, this);
			}
		}

		protected abstract void DispatchedTargetFinalized();

		Type IQueuedRemoval.LogOwner => GetType();

		string IQueuedRemoval.Description => "the callback of a collected subscriber";

		void IQueuedRemoval.Remove(object source)
		{
			try
			{
				DispatchedTargetFinalized();
			}
			catch (Exception e)
			{
				if (this.Log().IsEnabled(LogLevel.Error))
				{
					this.Log().Error("Failed to remove the callback of a collected subscriber", e);
				}
			}
		}
	}
}
