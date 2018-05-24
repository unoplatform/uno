using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Uno.Diagnostics.Eventing
{
	public static class EventProviderExtensions
	{
		/// <summary>
		/// This counter will be mapped in the ETL transform tool to an actual
		/// activity ID.
		/// </summary>
		private static long _activityCounter = 0;

		/// <summary>
		/// Writes an event in the specified provider.
		/// </summary>
		/// <param name="provider">The event provider to use</param>
		/// <param name="eventId">The event ID to use</param>
		/// <param name="payload">The payload for the event</param>
		public static void WriteEvent(this IEventProvider provider, int eventId, object[] payload = null)
		{
			provider.WriteEvent(new EventDescriptor(eventId), payload);
		}

		/// <summary>
		/// Writes an event in the specified provider.
		/// </summary>
		/// <param name="provider">The event provider to use</param>
		/// <param name="eventId">The event ID to use</param>
		/// <param name="opCode">The opcode for the event</param>
		/// <param name="payload">The payload for the event</param>
		public static void WriteEvent(this IEventProvider provider, int eventId, EventOpcode opCode, object[] payload = null)
		{
			provider.WriteEvent(new EventDescriptor(eventId: eventId, opcode: opCode), payload);
		}

		/// <summary>
		/// Write a single activity event, to be used with the stopping opcode later on.
		/// </summary>
		/// <param name="provider">The event provider to use</param>
		/// <param name="eventId">The event ID to write</param>
		/// <param name="opCode">The opcode of the event</param>
		/// <param name="payload">An optional payload</param>
		/// <returns>An activity identifier, to be used as a relatedActivityId, or activity id for the stopping event.</returns>
		public static EventActivity WriteEventActivity(this IEventProvider provider, int eventId, EventOpcode opCode, object[] payload = null)
		{
			var activity = CreateEventActivity();

			provider.WriteEvent(new EventDescriptor(eventId: eventId, opcode: opCode, activityId: activity?.Id ?? 0), payload);

			return activity;
		}

		/// <summary>
		/// Write a single activity event, to be used with the stopping opcode later on.
		/// </summary>
		/// <param name="provider">The event provider to use</param>
		/// <param name="eventId">The event ID to write</param>
		/// <param name="opCode">The opcode of the event</param>
		/// <param name="activityId">The current activity</param>
		/// <param name="payload">An optional payload</param>
		/// <returns>An activity identifier, to be used as a relatedActivityId, or activity id for the stopping event.</returns>
		public static void WriteEventActivity(this IEventProvider provider, int eventId, EventOpcode opCode, EventActivity activity, object[] payload = null)
		{
			provider.WriteEvent(new EventDescriptor(eventId: eventId, opcode: opCode, activityId: activity?.Id ?? 0), payload);
		}

		/// <summary>
		/// Writes an activity event, used to correlate start and stop events properly.
		/// </summary>
		/// <param name="provider">The event provider to use</param>
		/// <param name="startEventId">The starting event ID to write</param>
		/// <param name="stopEventId">The stopping event ID to write</param>
		/// <returns>The activity to be disposed when stopped</returns>
		public static DisposableEventActivity WriteEventActivity(this IEventProvider provider, int startEventId, int stopEventId)
		{
			if (provider.IsEnabled)
			{
				var activity = CreateEventActivity();

				provider.WriteEvent(
					new EventDescriptor(
						eventId: startEventId,
						opcode: EventOpcode.Start,
						activityId: activity?.Id ?? 0
					)
				);

				return new DisposableEventActivity(
					activity,
					() => provider.WriteEvent(
						new EventDescriptor(
							eventId: stopEventId,
							opcode: EventOpcode.Stop,
							activityId: activity?.Id ?? 0
						)
					)
				);
			}
			else
			{
				// the using statement handles null disposable properly.
				return null;
			}
		}

		/// <summary>
		/// Writes an activity event, used to correlate start and stop events properly.
		/// </summary>
		/// <param name="provider">The event provider to use</param>
		/// <param name="startEventId">The starting event ID to write</param>
		/// <param name="stopEventId">The stopping event ID to write</param>
		/// <param name="payload">The payload for this event</param>
		/// <returns>The activity to be disposed when stopped</returns>
		public static DisposableEventActivity WriteEventActivity(this IEventProvider provider, int startEventId, int stopEventId, object[] payload = null)
		{
			if (provider.IsEnabled)
			{
				var activity = CreateEventActivity();

				provider.WriteEvent(
					new EventDescriptor(
						eventId: startEventId,
						opcode: EventOpcode.Start,
						activityId: activity?.Id ?? 0
					),
					payload
				);

				return new DisposableEventActivity(
					activity,
					() => provider.WriteEvent(
						new EventDescriptor(
							eventId: stopEventId,
							opcode: EventOpcode.Stop,
							activityId: activity?.Id ?? 0
						),
						payload
					)
				);
			}
			else
			{
				// the using statement handles null disposable properly.
				return null;
			}
		}

		/// <summary>
		/// Writes an activity event, used to correlate start and stop events properly, with the ability to provide a related activity.
		/// </summary>
		/// <param name="provider">The event provider to use</param>
		/// <param name="startEventId">The starting event ID to write</param>
		/// <param name="stopEventId">The stopping event ID to write</param>
		/// <param name="relatedActivity">The new activity will be marked with this related activity.</param>
		/// <param name="payload">The payload for this event</param>
		/// <returns>The activity to be disposed when stopped</returns>
		public static DisposableEventActivity WriteEventActivity(
			this IEventProvider provider,
			int startEventId,
			int stopEventId,
			EventActivity relatedActivity,
			object[] payload = null
		)
		{
			if (provider.IsEnabled)
			{
				var activity = CreateEventActivity();

				provider.WriteEvent(
					new EventDescriptor(
						eventId: startEventId,
						opcode: EventOpcode.Start,
						activityId: activity?.Id ?? 0,
						relatedActivityId: relatedActivity?.Id ?? 0
					),
					payload
				);

				return new DisposableEventActivity(
					activity,
					() => provider.WriteEvent(
						new EventDescriptor(
							eventId: stopEventId,
							opcode: EventOpcode.Stop,
							activityId: activity?.Id ?? 0,
							relatedActivityId: relatedActivity?.Id ?? 0
						),
						payload
					)
				);
			}
			else
			{
				// the using statement handles null disposable properly.
				return null;
			}
		}

		private static EventActivity CreateEventActivity()
		{
			return new EventActivity(Interlocked.Increment(ref _activityCounter));
		}

		/// <summary>
		/// A EventActivity identifier.
		/// </summary>
		public class DisposableEventActivity : IDisposable
		{
			private Action _disposable;

			public DisposableEventActivity(EventActivity eventActivity, Action disposable)
			{
				_disposable = disposable;
				EventActivity = eventActivity;
			}

			public EventActivity EventActivity { get; }

			public void Dispose()
			{
				_disposable();
			}
		}
	}
}
