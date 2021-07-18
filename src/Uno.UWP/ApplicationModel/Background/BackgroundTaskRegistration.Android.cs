
#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Background
{

	public partial class BackgroundTaskRegistration : IBackgroundTaskRegistration, IBackgroundTaskRegistration2, IBackgroundTaskRegistration3
	{
		public string Name { get; internal set; }
		public IBackgroundTrigger Trigger { get; internal set; }

		public Guid TaskId { get; internal set; }

		private void RemoveTrigger()
		{
			RemoveTrigger(Name);
		}

		internal static void RemoveTrigger(string triggerName)
		{
			foreach(var item in AllTasksInternal)
			{
				if(item.Value.Name == triggerName)
				{
					AllTasksInternal.Remove(item.Key);
					return;
				}
			}

			// if we are here, it means that we didn't find such task.
		}

		public void Unregister(bool cancelTask)
		{
			if (Trigger is null)
			{
				return; // or throw 'internalerror' (cancelling non-existent intent)
			}

			int triggerType = 0;
			if (Trigger is TimeTrigger)
			{
				triggerType = 1;
			}
			if (Trigger is SystemTrigger)
			{
				triggerType = 2;
			}


			switch (triggerType)
			{
				case 1:
					// remove job 
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
								if (oneJob.Extras.GetString("TaskName", "") == Name)
								{
									jobScheduler.Cancel(oneJob.Id);
								}
							}
						}
					}
					break;
				case 2:
					// we only need to remove this trigger from list of trigger, no Android call is required
					break;
				default:
					throw new NotImplementedException("trying to Unregister trigger of unimplemented type");
			}

			RemoveTrigger();
		}

		public static IReadOnlyDictionary<Guid, IBackgroundTaskRegistration> AllTasks
		{ get => new Dictionary<Guid, IBackgroundTaskRegistration>(AllTasksInternal); }

		internal static Dictionary<Guid, IBackgroundTaskRegistration> AllTasksInternal = new Dictionary<Guid, IBackgroundTaskRegistration>();


	}
}

#endif
