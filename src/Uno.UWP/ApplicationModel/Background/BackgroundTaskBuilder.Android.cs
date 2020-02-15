
#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* 

 When adding new types of Triggers, ensure that:
  * class for this Trigger is defined;
  * here, in SetTrigger(), set _triggerType to next available int;
  * add 'case' for your Trigger in:
	* here, in Register()
	* in BackgroundTaskRegistration:Unregister
	* in BackgroundActivatedEventArgs:ctor
	
  You can use Windows.Extensions.PermissionsHelper.IsDeclaredInManifest(permission)  to verify that required permissions are set

*/

namespace Windows.ApplicationModel.Background
{
	public partial class BackgroundTaskBuilder
	{
		private IBackgroundTrigger _trigger;
		internal int _triggerType = 0;
		private List<IBackgroundCondition> _conditions = new List<IBackgroundCondition>();

		// two variables for Android to be set by UWP application
		internal static Type _jobHandler = null;
		internal static Android.Content.BroadcastReceiver _broadcastReceiver = null;
		internal static bool _receiverRegistered = false;

		// new method (not from UWP), but needed - to be called from Uno project template
		public static void UnoInitTriggers(Type jobHandler, Android.Content.BroadcastReceiver broadcastReceiver)
		{
			_jobHandler = jobHandler;
			_broadcastReceiver = broadcastReceiver;
		}

		public string Name { get; set; }
		public BackgroundTaskBuilder()
		{
		}

		public void AddCondition(IBackgroundCondition condition)
		{
			_conditions.Add(condition);
		}

		public void SetTrigger(IBackgroundTrigger trigger)
		{
			_trigger = trigger;

			if (trigger is TimeTrigger)
				_triggerType = 1;	// TimeTrigger
			if(trigger is SystemTrigger)
				_triggerType = 2;   // any SystemTrigger
		}

		private static int GetIdForJob(string taskName)
		{
			// use first available id
			int maxUsed = 0;

			var jobScheduler = (Android.App.Job.JobScheduler)Android.App.Application.Context.GetSystemService(
						Android.Content.Context.JobSchedulerService);
			var currentJobs = jobScheduler.AllPendingJobs;
			foreach (var oneJob in currentJobs)
			{
				if (oneJob.Extras != null)
				{
					if (oneJob.Extras.GetBoolean("UnoUWPEmulation", false))
					{
						// this is "our", so we can expect our variables
						if (oneJob.Extras.GetString("TaskName", "") == taskName)
						{
							return oneJob.Id;
						}
					}
				}

				maxUsed = Math.Max(maxUsed, oneJob.Id);
			}

			return maxUsed + 1;
		}


		private void RegisterTimeTrigger()
		{
			// method called from Register(), if we are registering TimeTrigger

			var serviceComponent = new Android.Content.ComponentName(
				Android.App.Application.Context, Java.Lang.Class.FromType(_jobHandler));
			int jobId = GetIdForJob(Name);
			var jobBuilder = new Android.App.Job.JobInfo.Builder(jobId, serviceComponent);

			var jobParams = new Android.OS.PersistableBundle();
			jobParams.PutString("TaskName", Name);
			jobParams.PutBoolean("UnoUWPEmulation", true);

			jobParams.PutInt("TriggerType", _triggerType);

			// set trigger data
			var timeTrigger = _trigger as TimeTrigger;
			jobParams.PutInt("Freshness", (int)timeTrigger.FreshnessTime);
			jobParams.PutBoolean("OneShot", timeTrigger.OneShot);

			if (timeTrigger.OneShot)
			{
				jobBuilder.SetMinimumLatency(timeTrigger.FreshnessTime * 60 * 1000);    // start after FreshnessTime minues
				jobBuilder.SetOverrideDeadline(timeTrigger.FreshnessTime * 60 * 1000 + 15 * 60 * 1000); // but no later than 15 minutes later
			}
			else
			{   // Android has two SetPeriodic():
				// SetPeriodic(long intervalMillis)
				// fires at once, and after intervalMillis - so, fires at begin of time interval
				// SetPeriodic(long intervalMillis, long flexMillis)
				// fires within flexMillis window at end of intervalMillis period (at end of interval)
				jobBuilder.SetPeriodic(timeTrigger.FreshnessTime * 60 * 1000, 1 * 60 * 1000);
			}

			jobBuilder.SetExtras(jobParams);

			foreach (SystemCondition condition in _conditions)
			{
				switch (condition.ConditionType)
				{
					case SystemConditionType.BackgroundWorkCostNotHigh:
						jobBuilder.SetRequiresBatteryNotLow(true);
						break;
					case SystemConditionType.FreeNetworkAvailable:
						jobBuilder.SetRequiredNetworkType(Android.App.Job.NetworkType.Unmetered);
						break;
					case SystemConditionType.InternetAvailable:
						jobBuilder.SetRequiredNetworkType(Android.App.Job.NetworkType.Any);
						break;
					default:
						throw new ArgumentException("BackgroundTaskBuilder.Register(): unimplemented Condition for TimeTrigger");
				}
			}

			var jobScheduler =
				(Android.App.Job.JobScheduler)Android.App.Application.Context.GetSystemService(
					Android.Content.Context.JobSchedulerService);

			var scheduleResult = jobScheduler.Schedule(jobBuilder.Build());

			if (scheduleResult == Android.App.Job.JobScheduler.ResultFailure)
			{
				throw new ArgumentException("BackgroundTaskBuilder.Register(): Android JobScheduler returned failure");
			}

		}

		private void RegisterSystemTrigger()
		{
			// method called from Register(), if we are registering SystemTrigger
			// check subtype
			switch ((_trigger as SystemTrigger).TriggerType)
			{ // supported types
				case SystemTriggerType.ServicingComplete:
				case SystemTriggerType.SmsReceived:
				case SystemTriggerType.TimeZoneChange:
				case SystemTriggerType.UserAway:
				case SystemTriggerType.UserPresent:
					break;
				default:    // all other types: unsupported
					throw new NotImplementedException("Unimplemented type of SystemTrigger");
			}

			// maybe throw if any of _conditions is unimplemented in this type of trigger
			foreach (SystemCondition condition in _conditions)
			{
				switch (condition.ConditionType)
				{
					default:
						throw new ArgumentException("unimplemented Condition for SystemTrigger");
				}
			}

			if (!_receiverRegistered)
			{   // none of them is STICKY
				var filter = new Android.Content.IntentFilter();
				filter.AddAction(Android.Provider.Telephony.Sms.Intents.SmsReceivedAction);
				filter.AddAction(Android.Content.Intent.ActionTimezoneChanged);
				filter.AddAction(Android.Content.Intent.ActionUserPresent);
				filter.AddAction(Android.Content.Intent.ActionMyPackageReplaced);
				filter.AddAction(Android.Content.Intent.ActionScreenOff);

				Android.App.Application.Context.RegisterReceiver(_broadcastReceiver, filter);
				_receiverRegistered = true;
			}

		}

		public BackgroundTaskRegistration Register()
		{
			if (_trigger is null)
			{
				throw new ArgumentException("BackgroundTaskBuilder.Register(): cannot Register trigger when Trigger is null");
			}

			// prepare return value
			var registration = new BackgroundTaskRegistration();
			registration.Name = Name;
			registration.Trigger = _trigger;

			switch (_triggerType)
			{
				case 1: // TimeTrigger
					if (_jobHandler is null)
					{
						throw new ArgumentException("BackgroundTaskBuilder.Register(): to register TimeTrigger, _jobHandler has to be set - use UnoInitTriggers() method (probably you are using App.xaml.cs from older Uno version)");
					}

					RegisterTimeTrigger();

					break;
				case 2: // SystemTrigger
					if (_broadcastReceiver is null)
					{
						throw new ArgumentException("BackgroundTaskBuilder.Register(): to register SystemTrigger, _broadcastReceiver has to be set - use UnoInitTriggers() method (probably you are using App.xaml.cs from older Uno version)");
					}

					RegisterSystemTrigger();

					break;

				default:
					throw new ArgumentException("BackgroundTaskBuilder.Register(): cannot register this trigger (unimplemented)");
			}

			// after all checking, we can store trigger data

			// Doc says: "The chance that the value of the new Guid will be all zeros or equal to any other Guid is very low"
			// so it is possible, and test for Empty costs us nothing
			// see https://softwareengineering.stackexchange.com/questions/274473/is-it-worth-even-checking-to-see-if-guid-newguid-is-guid-empty 
			do
			{
				registration.TaskId = Guid.NewGuid();
			} while (registration.TaskId == Guid.Empty);

			BackgroundTaskRegistration.AllTasksInternal.Add(registration.TaskId, registration);

			return registration;
		}

	}

}

#endif
