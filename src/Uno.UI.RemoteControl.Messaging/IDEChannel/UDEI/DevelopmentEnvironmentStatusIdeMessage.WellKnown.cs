#nullable enable
using System;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

public partial record DevelopmentEnvironmentStatusIdeMessage
{
	public static class DevServer
	{
		private static readonly Command _doc = Command.OpenBrowser("Details", new Uri("https://aka.platform.uno/?udei-why"));

		/// <summary>
		/// Indicates dev-server is starting successfully.
		/// </summary>
		public static DevelopmentEnvironmentStatusIdeMessage Starting { get; } = new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Initializing,
			"Starting",
			null,
			null,
			[_doc]);

		/// <summary>
		/// Indicates dev-server process has been killed (and will not restart by its own).
		/// </summary>
		public static DevelopmentEnvironmentStatusIdeMessage Failed(Exception error) => new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Error,
			"Not running",
			null,
			null,
			[_doc]);

		/// <summary>
		/// Indicates dev-server is restarting.
		/// </summary>
		public static DevelopmentEnvironmentStatusIdeMessage Restarting { get; } = new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Warning,
			"Restarting",
			null,
			null,
			[_doc]);

		/// <summary>
		/// Indicates dev-server didn't connected back to IDE channel in the given delay.
		/// </summary>
		public static DevelopmentEnvironmentStatusIdeMessage Timeout { get; } = new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Error,
			"Timeout",
			"Dev-server didn't connected back to IDE in the given delay",
			null,
			[_doc]);

		public static DevelopmentEnvironmentStatusIdeMessage Ready { get; } = new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Ready,
			"Dev-server is ready",
			null,
			null,
			[_doc]);

		// Factory helpers for messages that include runtime details
		public static DevelopmentEnvironmentStatusIdeMessage FailedToDiscover(string? details) => new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Warning,
			"Failed to discover add-ins",
			details,
			null,
			[_doc]);

		public static DevelopmentEnvironmentStatusIdeMessage FailedToLoad(string? details) => new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Warning,
			"Failed to load add-ins",
			details,
			null,
			[_doc]);
	}
}
