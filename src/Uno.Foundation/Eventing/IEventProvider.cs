using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Diagnostics.Eventing
{
	/// <summary>
	/// An event provider that sends event to the tracing subsystem
	/// </summary>
    public interface IEventProvider
    {
		/// <summary>
		/// Defines if the provider is enabled.
		/// </summary>
		bool IsEnabled { get; }

		/// <summary>
		/// Writes a string message to the provider
		/// </summary>
		/// <param name="eventMessage">The string to write</param>
		/// <returns>True if the write succeeded, otherwise false.</returns>
		bool WriteMessageEvent(string eventMessage);

		/// <summary>
		/// Writes a full event descriptor with a message to the provider
		/// </summary>
		/// <param name="eventDescriptor">An event descriptor</param>
		/// <param name="data">A string to add as payload</param>
		/// <returns>True if the write succeeded, otherwise false.</returns>
		bool WriteEvent(EventDescriptor eventDescriptor, string data);

		/// <summary>
		/// Writes a full event descriptor with an array of objects to the provider
		/// </summary>
		/// <param name="eventDescriptor">An event descriptor</param>
		/// <param name="data">A string to add as payload</param>
		/// <returns>True if the write succeeded, otherwise false.</returns>
		/// <remarks>Data can be of Int32, Int64 or String.</remarks>
		bool WriteEvent(EventDescriptor eventDescriptor, params object[] data);
	}
}
