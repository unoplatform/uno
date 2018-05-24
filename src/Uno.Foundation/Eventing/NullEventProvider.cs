using System;

namespace Uno.Diagnostics.Eventing
{
	/// <summary>
	/// Defines a disabled provider
	/// </summary>
	public class NullEventProvider : IEventProvider
	{
		public static IEventProvider Instance { get; } = new NullEventProvider();

		bool IEventProvider.IsEnabled { get; } = false;

		bool IEventProvider.WriteMessageEvent(string eventMessage)
		{
			return false;
		}

		bool IEventProvider.WriteEvent(EventDescriptor eventDescriptor, object[] data)
		{
			return false;
		}

		bool IEventProvider.WriteEvent(EventDescriptor eventDescriptor, string data)
		{
			return false;
		}
	}
}