#nullable enable
using System;

namespace Uno.UI.RemoteControl.Messaging.IdeChannel;

partial record DevelopmentEnvironmentStatusIdeMessage
{
	public static class DevServer
	{
		public static readonly Command Doc = Command.OpenBrowser("Learn More", new Uri("https://aka.platform.uno/dev-server"));
		public static readonly Command Troubleshoot = Command.OpenBrowser("Troubleshoot", new Uri("https://aka.platform.uno/dev-server-troubleshooting"));
		public static readonly Command Restart = new("Restart", "uno.dev_server.restart");

		/// <summary>
		/// Indicates dev-server is starting successfully.
		/// </summary>
		public static DevelopmentEnvironmentStatusIdeMessage Starting { get; } = new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Initializing,
			"Starting...",
			null,
			null,
			[Doc]);

		/// <summary>
		/// Indicates dev-server process is unable to start (and will not restart by its own).
		/// </summary>
		public static DevelopmentEnvironmentStatusIdeMessage Failed(Exception error) => new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Error,
			"Failed",
			error.Message,
			error.StackTrace,
			[Restart, Doc]);

		/// <summary>
		/// Indicates dev-server is restarting.
		/// </summary>
		public static DevelopmentEnvironmentStatusIdeMessage Restarting { get; } = new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Warning,
			"Restarting...",
			null,
			null,
			[Doc]);

		/// <summary>
		/// Indicates dev-server didn't connected back to IDE channel in the given delay.
		/// </summary>
		public static DevelopmentEnvironmentStatusIdeMessage Timeout { get; } = new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Error,
			"Timeout",
			"Dev-server didn't connect back to IDE in the given delay",
			null,
			[Restart, Doc]);

		/// <summary>
		/// Indicates dev-server is being disabled (as it's not a uno solution).
		/// </summary>
		public static DevelopmentEnvironmentStatusIdeMessage NotStarted { get; } = new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Error,
			"Uno solution not found",
			null,
			null,
			[Doc]);

		public static DevelopmentEnvironmentStatusIdeMessage Ready { get; } = new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Ready,
			"Ready",
			null,
			null,
			[Doc]);

		// Factory helpers for messages that include runtime details
		public static DevelopmentEnvironmentStatusIdeMessage FailedToDiscoverAddIns(string? details) => new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Warning,
			"Studio unavailable",
			details,
			null,
			[Troubleshoot, Doc]);

		public static DevelopmentEnvironmentStatusIdeMessage FailedToLoadAddIns(string? details) => new(
			DevelopmentEnvironmentComponent.DevServer,
			DevelopmentEnvironmentStatus.Warning,
			"Unable to load Studio",
			details,
			null,
			[Troubleshoot, Doc]);
	}
}
