using System;

namespace Uno.UI.RemoteControl.Server.Telemetry
{
	/// <summary>
	/// Configures telemetry for an assembly. This attribute must be applied at the assembly level
	/// for telemetry to work properly.
	/// </summary>
	/// <remarks>
	/// The telemetry system uses this attribute to configure how events are tracked and reported.
	/// When the telemetry system is initialized, it looks for this attribute on the assembly
	/// to determine the instrumentation key and event prefix to use.
	/// 
	/// Example usage:
	/// [assembly: Telemetry("MyApp", EventsPrefix = "myapp/module")]
	/// </remarks>
	[AttributeUsage(AttributeTargets.Assembly)]
	public class TelemetryAttribute(string instrumentationKey) : Attribute
	{
		/// <summary>
		/// Gets the instrumentation key used to identify the telemetry source.
		/// This key is used to route telemetry data to the appropriate destination.
		/// </summary>
		public string InstrumentationKey { get; } = instrumentationKey;

		/// <summary>
		/// Gets or sets the prefix to apply to all event names generated from this assembly.
		/// If not specified, a default prefix based on the assembly name will be used.
		/// </summary>
		public string? EventsPrefix { get; set; } = string.Empty;
	}
}
