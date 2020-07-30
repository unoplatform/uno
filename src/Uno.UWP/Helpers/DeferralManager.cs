using System;
using Windows.Foundation;
using Windows.UI.Core;

namespace Uno.Helpers
{
	/// <summary>
	/// Handles completion of deferrals. The deferred action is completed when all deferral objects that were taken have called Complete().
	/// </summary>
	/// <typeparam name="T"></typeparam>
	internal class DeferralManager<T>
	{
		private readonly Func<DeferralCompletedHandler, T> _deferralFactory;

		/// <summary>
		/// Start the count at 1, this ensures the deferral won't be completed until all subscribers to the corresponding event have had a
		/// chance to take out a deferral object.
		/// </summary>
		private int _deferralsCount = 1;

		public DeferralManager(Func<DeferralCompletedHandler, T> deferralFactory)
		{
			_deferralFactory = deferralFactory;
		}

		internal event EventHandler Completed;

		public T GetDeferral()
		{
			_deferralsCount++;
			var isCompleted = false;
			return _deferralFactory(OnDeferralCompleted);

			void OnDeferralCompleted()
			{
#if !__WASM__ // Disable check on WASM until threading is supported
				CoreDispatcher.CheckThreadAccess();
#endif
				if (isCompleted)
				{
					throw new InvalidOperationException("Deferral already completed.");
				}

				isCompleted = true;
				DeferralCompleted();
			}
		}

		/// <summary>
		/// This must be called after the event which gives out the referral has finished being raised.
		/// </summary>
		internal void EventRaiseCompleted() => DeferralCompleted();

		private void DeferralCompleted()
		{
			_deferralsCount--;
			if (_deferralsCount <= 0)
			{
				Completed?.Invoke(this, EventArgs.Empty);
			}
		}
	}
}
