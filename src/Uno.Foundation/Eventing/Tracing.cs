using System;
using System.Collections.Generic;
using System.Text;

namespace Uno.Diagnostics.Eventing
{
	/// <summary>
	/// A common entry point for all event tracing APIs
	/// </summary>
	public static class Tracing
	{
		/// <summary>
		/// Provides the current EventProvider factory.
		/// </summary>
		public static IEventProviderFactory Factory { get; set; }

		/// <summary>
		/// Provides the enabled state for the whole tracing subsystem.
		/// </summary>
		public static bool IsEnabled { get; set; }
		
		/// <summary>
		/// Gets an event provider for the specified ID
		/// </summary>
		/// <param name="provider">The ID of the provider</param>
		/// <returns>An event provider</returns>
		public static IEventProvider Get(Guid provider)
		{
			if(Factory != null && IsEnabled)
			{
				return Factory.GetProvider(provider);
			}
			else
			{
				return NullEventProvider.Instance;
			}
		}
	}
}
