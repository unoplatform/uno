using Android.App;
using System;
using System.Threading;
using System.Threading.Tasks;
using Fragment = AndroidX.Fragment.App.Fragment;


namespace Uno.UI
{
	public interface IFragmentTracker
	{
		IObservable<Fragment> ObserveCreated();

		IObservable<Fragment> ObserveStart();

		IObservable<Fragment> ObservePause();

		IObservable<Fragment> ObserveResume();

		IObservable<Fragment> ObserveViewCreated();

		IObservable<Fragment> ObserveStop();

		IObservable<Fragment> ObserveDestroyed();

		Task<Fragment> GetCurrent(CancellationToken ct);

		void PushCreated(Fragment fragment);

		void PushStart(Fragment fragment);

		void PushPause(Fragment fragment);

		void PushResume(Fragment fragment);
		void PushViewCreated(Fragment fragment);

		void PushStop(Fragment fragment);

		void PushDestroyed(Fragment fragment);
	}
}
