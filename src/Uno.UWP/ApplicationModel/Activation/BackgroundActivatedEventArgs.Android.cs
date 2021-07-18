
#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Activation
{
	public partial class BackgroundActivatedEventArgs : IBackgroundActivatedEventArgs
	{
		public Background.IBackgroundTaskInstance TaskInstance { get; internal set; }

		public BackgroundActivatedEventArgs()
		{
			// protection for: Error : Removed method System.Void Windows.ApplicationModel.Activation.BackgroundActivatedEventArgs..ctor() not found in ignore set
			// not adding to diffignore, because other platforms should have this method.
		}


		// below are two "API addition" methods that are called from Shared/app.xaml.cs

		public BackgroundActivatedEventArgs(Android.App.Job.JobParameters jobParams)
		{
			// running from JobScheduler (time.trigger)

			// extract all data
			var extras = jobParams.Extras;
			if(extras is null || !extras.GetBoolean("UnoUWPEmulation", false))
			{	// no data, or no our signature
				throw new ArgumentNullException("BackgroundActivatedEventArgs - no expected data in jobParams");
			}

			string taskName = extras.GetString("TaskName", "");
			int triggerType = extras.GetInt("TriggerType", 0);

			// we have to re-create BackgroundTaskRegistration to send it to OnBackgroundActivated
			var taskRegistration = new Windows.ApplicationModel.Background.BackgroundTaskRegistration();
			taskRegistration.Name = taskName;

			switch (triggerType)
			{
				case 1: // TimeTrigger
					int freshness = extras.GetInt("Freshness", 30);
					bool oneShot = extras.GetBoolean("OneShot", true);
					if(oneShot)
					{
						Background.BackgroundTaskRegistration.RemoveTrigger(taskRegistration.Name);
					}
					taskRegistration.Trigger = new Windows.ApplicationModel.Background.TimeTrigger((uint)freshness, oneShot);
					break;
				default:
					throw new ArgumentException("BackgroundActivatedEventArgs - unhandled TriggerType");

			}

			// creating IBackgroundTaskInstance
			var taskInstance = new Windows.ApplicationModel.Background.BackgroundTaskInstance();
			taskInstance.Task = taskRegistration;

			// creating BackgroundActivatedEventArgs
			TaskInstance = taskInstance;
		}

		public BackgroundActivatedEventArgs(Android.Content.Intent intent)
		{
			// running from BroadcastReceiver (systemtrigger)

			// extract all data
			// our assumption: we handle SystemTrigger here, and not other
			Background.SystemTriggerType subtype = Background.SystemTriggerType.Invalid;

			switch (intent.Action)
			{
				case Android.Provider.Telephony.Sms.Intents.SmsReceivedAction:
					subtype = Background.SystemTriggerType.SmsReceived;
					break;
				case Android.Content.Intent.ActionTimezoneChanged:
					subtype = Background.SystemTriggerType.TimeZoneChange;
					break;
				case Android.Content.Intent.ActionUserPresent:
					subtype = Background.SystemTriggerType.UserPresent;
					break;
				case Android.Content.Intent.ActionMyPackageReplaced:
					subtype = Background.SystemTriggerType.ServicingComplete;
					break;
				case Android.Content.Intent.ActionScreenOff:
					subtype = Background.SystemTriggerType.UserAway;
					break;

			}

			Windows.ApplicationModel.Background.BackgroundTaskRegistration taskRegistration = null;

			// try to find this trigger
			foreach (var registration in Background.BackgroundTaskRegistration.AllTasksInternal)
			{
				var registrationValue = (Background.BackgroundTaskRegistration)registration.Value;
				var systemTrigger = registrationValue.Trigger as Background.SystemTrigger;
				if (systemTrigger != null)
				{
					if (systemTrigger.TriggerType == subtype)
					{
						taskRegistration = registrationValue;
						if (systemTrigger.OneShot)
						{ // and forget about it, if it was OneShot
							Background.BackgroundTaskRegistration.AllTasksInternal.Remove(registration.Key);
						}
						break;
					}
				}
			}


			// creating IBackgroundTaskInstance
			Windows.ApplicationModel.Background.BackgroundTaskInstance taskInstance = null;

			if (taskRegistration != null)
			{
				taskInstance = new Windows.ApplicationModel.Background.BackgroundTaskInstance();
				taskInstance.Task = taskRegistration;
			}

			// creating BackgroundActivatedEventArgs - TaskInstance can be null, if we received intent but doesn't want such UWP trigger
			TaskInstance = taskInstance;
		}

	}
}

#endif
