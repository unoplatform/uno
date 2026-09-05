#nullable enable

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Windows.ApplicationModel.Background;

internal static class BackgroundTaskRunner
{
	private static readonly TimeSpan CancellationGracePeriod =
		TimeSpan.FromSeconds(5);

	internal static int Run(Guid taskId)
	{
		var record = BackgroundTaskRegistrationStore.Find(taskId);
		if (record is null)
		{
			Console.Error.WriteLine(
				$"Background task registration '{taskId}' was not found.");
			return 2;
		}

		var instanceId = Guid.NewGuid();
		var exitCode = 0;
		string? errorMessage = null;
		IBackgroundTask? task = null;

		var taskType = ResolveTaskType(record.TaskEntryPoint);
		if (taskType is null || !typeof(IBackgroundTask).IsAssignableFrom(taskType))
		{
			errorMessage =
				$"The background task entry point '{record.TaskEntryPoint}' "
				+ "could not be loaded as an IBackgroundTask.";
			exitCode = 3;
		}
		else
		{
			try
			{
				task = (IBackgroundTask?)Activator.CreateInstance(taskType)
					?? throw new InvalidOperationException(
						$"The background task entry point '{record.TaskEntryPoint}' "
							+ "could not be created.");
			}
			catch (Exception error) when (
				error is MissingMethodException
					or MemberAccessException
					or TargetInvocationException
					or InvalidOperationException)
			{
				errorMessage = error.ToString();
				exitCode = 4;
				Console.Error.WriteLine(error);
			}
		}

		if (task is not null)
		{
			var registration = new BackgroundTaskRegistration(record);
			var instance = new BackgroundTaskInstance(registration, record.Trigger);
			instanceId = instance.InstanceId;
			using var termination = CreateTerminationRegistration(instance);
			ConsoleCancelEventHandler cancelHandler = (_, args) =>
			{
				args.Cancel = true;
				instance.Cancel(BackgroundTaskCancellationReason.Terminating);
			};
			Console.CancelKeyPress += cancelHandler;
			global::System.Threading.Tasks.Task? completion = null;

			try
			{
				var execution = global::System.Threading.Tasks.Task.Run(() =>
				{
					try
					{
						task.Run(instance);
					}
					finally
					{
						instance.MarkRunReturned();
					}
				});
				completion = global::System.Threading.Tasks.Task.WhenAll(
					execution,
					instance.Completion);
				var first = global::System.Threading.Tasks.Task
					.WhenAny(
						execution,
						completion,
						instance.CancellationRequested)
					.GetAwaiter()
					.GetResult();
				if (first == execution && execution.IsFaulted)
				{
					execution.GetAwaiter().GetResult();
				}

				if (first != completion && first != instance.CancellationRequested)
				{
					first = global::System.Threading.Tasks.Task
						.WhenAny(completion, instance.CancellationRequested)
						.GetAwaiter()
						.GetResult();
				}

				if (first == instance.CancellationRequested &&
					!completion.Wait(CancellationGracePeriod))
				{
					errorMessage =
						"Background task cancellation timed out after "
						+ $"{CancellationGracePeriod.TotalSeconds:0} seconds.";
					exitCode = 6;
				}
				else
				{
					completion.GetAwaiter().GetResult();
				}
			}
			catch (Exception error)
			{
				errorMessage = error.ToString();
				exitCode = 1;
				Console.Error.WriteLine(error);
			}
			finally
			{
				Console.CancelKeyPress -= cancelHandler;
				// The WhenAll wrapper faults with the task body; observe it so the failure is
				// not re-raised as an unobserved task exception during finalization.
				_ = completion?.Exception;
			}

			if (instance.CancellationRequested.IsCompleted &&
				errorMessage is null)
			{
				errorMessage = "Background task was cancelled.";
				exitCode = 6;
			}
		}

		if (record.Trigger.OneShot)
		{
			try
			{
				BackgroundTaskRegistrationStore.CompleteOneShot(record.TaskId);
			}
			catch (Exception error) when (
				error is IOException
					or UnauthorizedAccessException
					or InvalidOperationException
					or global::System.ComponentModel.Win32Exception)
			{
				Console.Error.WriteLine(
					$"The one-shot background task could not be unregistered: {error}");
				errorMessage = JoinErrors(
					errorMessage,
					"The one-shot registration could not be removed: "
					+ error.Message);
				exitCode = 5;
			}
		}

		WriteCompletion(record, instanceId, errorMessage);
		return errorMessage is null ? 0 : exitCode;
	}

	private static Type? ResolveTaskType(string taskEntryPoint)
	{
		if (Type.GetType(taskEntryPoint, throwOnError: false) is { } resolved)
		{
			return resolved;
		}

		if (Package.EntryAssembly?.GetType(
			taskEntryPoint,
			throwOnError: false,
			ignoreCase: false) is { } applicationType)
		{
			return applicationType;
		}

		if (Assembly.GetEntryAssembly()?.GetType(
			taskEntryPoint,
			throwOnError: false,
			ignoreCase: false) is { } entryType)
		{
			return entryType;
		}

		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (assembly.GetType(
				taskEntryPoint,
				throwOnError: false,
				ignoreCase: false) is { } assemblyType)
			{
				return assemblyType;
			}
		}

		return null;
	}

	private static PosixSignalRegistration? CreateTerminationRegistration(
		BackgroundTaskInstance instance)
	{
		if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
		{
			return null;
		}

		return PosixSignalRegistration.Create(
			PosixSignal.SIGTERM,
			context =>
			{
				context.Cancel = true;
				instance.Cancel(BackgroundTaskCancellationReason.Terminating);
			});
	}

	private static void WriteCompletion(
		BackgroundTaskRegistrationRecord record,
		Guid instanceId,
		string? errorMessage)
	{
		try
		{
			BackgroundTaskRegistrationStore.WriteEvent(
				new BackgroundTaskEvent(
					BackgroundTaskEventKind.Completed,
					record.TaskId,
					instanceId,
					Progress: 0,
					errorMessage));
		}
		catch (Exception error) when (
			error is IOException or UnauthorizedAccessException)
		{
			// Completion notification is best effort and is only observed by a subscribed
			// foreground process; a failed write must not change the task's own outcome.
			Console.Error.WriteLine(
				$"The background task completion could not be published: {error}");
		}
	}

	private static string JoinErrors(string? first, string second)
		=> string.IsNullOrWhiteSpace(first)
			? second
			: first + Environment.NewLine + second;
}
