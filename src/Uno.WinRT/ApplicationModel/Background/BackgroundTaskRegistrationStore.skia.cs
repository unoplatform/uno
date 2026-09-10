#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Uno.Foundation.Logging;
using Windows.Storage;

namespace Windows.ApplicationModel.Background;

internal static class BackgroundTaskRegistrationStore
{
	private const int FormatVersion = 1;
	private const int MaximumRegistrations = 10_000;
	private const int MaximumArguments = 128;
	private const long MaximumStoreLength = 16 * 1024 * 1024;
	private static readonly object Gate = new();

	internal static string StorageDirectory =>
		Path.Combine(ApplicationData.Current.LocalFolder.Path, "BackgroundTasks");

	internal static string EventsDirectory =>
		Path.Combine(StorageDirectory, "Events");

	internal static string StorePath =>
		Path.Combine(StorageDirectory, "registrations.dat");

	private static string LockPath =>
		Path.Combine(StorageDirectory, "registrations.lock");

	internal static IReadOnlyList<BackgroundTaskRegistrationRecord> GetAll()
	{
		lock (Gate)
		{
			using var fileLock = AcquireFileLock();
			if (BackgroundTaskScheduler.TryGetExtension(out var scheduler))
			{
				scheduler.Reconcile();
			}

			return ReadStore(StorePath);
		}
	}

	internal static BackgroundTaskRegistrationRecord? Find(Guid taskId)
		=> GetAll().FirstOrDefault(registration => registration.TaskId == taskId);

	internal static BackgroundTaskRegistration Register(
		BackgroundTaskRegistrationRecord registration)
	{
		var scheduler = BackgroundTaskScheduler.GetRequiredExtension();
		lock (Gate)
		{
			using var fileLock = AcquireFileLock();
			var registrations = ReadStore(StorePath);
			if (registrations.Any(existing => existing.TaskId == registration.TaskId))
			{
				throw new InvalidOperationException(
					$"Background task {registration.TaskId} is already registered.");
			}

			if (registrations.Count >= MaximumRegistrations)
			{
				throw new InvalidOperationException(
					$"No more than {MaximumRegistrations} background tasks can be registered.");
			}

			scheduler.Register(registration);
			try
			{
				WriteStore(StorePath, registrations.Append(registration));
			}
			catch (Exception persistenceError) when (
				persistenceError is IOException
					or UnauthorizedAccessException
					or InvalidOperationException)
			{
				try
				{
					scheduler.Unregister(registration, cancelTask: true);
				}
				catch (Exception rollbackError) when (IsSchedulerFailure(rollbackError))
				{
					throw new AggregateException(
						"The background task was registered with the operating system, "
						+ "but its registration could not be saved or rolled back.",
						persistenceError,
						rollbackError);
				}

				throw;
			}
		}

		return new BackgroundTaskRegistration(registration);
	}

	internal static void Unregister(Guid taskId, bool cancelTask)
	{
		var scheduler = BackgroundTaskScheduler.GetRequiredExtension();
		lock (Gate)
		{
			using var fileLock = AcquireFileLock();
			var registrations = ReadStore(StorePath);
			var registration = registrations.FirstOrDefault(item => item.TaskId == taskId);
			if (registration is null)
			{
				return;
			}

			scheduler.Unregister(registration, cancelTask);
			try
			{
				WriteStore(
					StorePath,
					registrations.Where(item => item.TaskId != taskId));
			}
			catch (Exception persistenceError) when (
				persistenceError is IOException or UnauthorizedAccessException)
			{
				try
				{
					scheduler.Register(registration);
				}
				catch (Exception rollbackError) when (IsSchedulerFailure(rollbackError))
				{
					throw new AggregateException(
						"The background task was removed from the operating system, "
						+ "but its saved registration could not be removed or restored.",
						persistenceError,
						rollbackError);
				}

				throw;
			}
		}
	}

	internal static void CompleteOneShot(Guid taskId)
	{
		var scheduler = BackgroundTaskScheduler.GetRequiredExtension();
		lock (Gate)
		{
			using var fileLock = AcquireFileLock();
			var registrations = ReadStore(StorePath);
			var registration = registrations.FirstOrDefault(item => item.TaskId == taskId);
			if (registration is null)
			{
				return;
			}

			WriteStore(
				StorePath,
				registrations.Where(item => item.TaskId != taskId));
			try
			{
				scheduler.CompleteOneShot(registration);
			}
			catch (Exception schedulerError) when (IsSchedulerFailure(schedulerError))
			{
				try
				{
					WriteStore(StorePath, registrations);
				}
				catch (Exception rollbackError) when (
					rollbackError is IOException or UnauthorizedAccessException)
				{
					throw new AggregateException(
						"The one-shot background task could not be removed from the "
						+ "operating system, and its saved registration could not be restored.",
						schedulerError,
						rollbackError);
				}

				throw;
			}
		}
	}

	internal static void WriteEvent(BackgroundTaskEvent taskEvent)
	{
		Directory.CreateDirectory(EventsDirectory);
		var fileName =
			$"{taskEvent.TaskId:N}-{DateTime.UtcNow.Ticks:D19}-{Guid.NewGuid():N}.event";
		var path = Path.Combine(EventsDirectory, fileName);
		var temporaryPath = path + ".tmp";

		try
		{
			using (var stream = new FileStream(
				temporaryPath,
				FileMode.CreateNew,
				FileAccess.Write,
				FileShare.None,
				4096,
				FileOptions.WriteThrough))
			using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
			{
				writer.Write((byte)taskEvent.Kind);
				writer.Write(taskEvent.TaskId.ToByteArray());
				writer.Write(taskEvent.InstanceId.ToByteArray());
				writer.Write(taskEvent.Progress);
				writer.Write(taskEvent.ErrorMessage ?? string.Empty);
				writer.Flush();
				stream.Flush(flushToDisk: true);
			}

			File.Move(temporaryPath, path);
			TrimEvents(taskEvent.TaskId);
		}
		finally
		{
			if (File.Exists(temporaryPath))
			{
				File.Delete(temporaryPath);
			}
		}
	}

	internal static BackgroundTaskEvent ReadEvent(string path)
	{
		// Retention trimming deletes events while a subscriber may still be reading them, so
		// the reader must not block deletion.
		using var stream = File.Open(
			path,
			new FileStreamOptions
			{
				Mode = FileMode.Open,
				Access = FileAccess.Read,
				Share = FileShare.Read | FileShare.Delete
			});
		using var reader = new BinaryReader(stream, Encoding.UTF8);
		var kind = (BackgroundTaskEventKind)reader.ReadByte();
		if (!Enum.IsDefined(kind))
		{
			throw new InvalidDataException(
				"The background task event has an unsupported kind.");
		}

		var taskId = ReadGuid(reader);
		var instanceId = ReadGuid(reader);
		var progress = reader.ReadUInt32();
		var errorMessage = reader.ReadString();
		return new BackgroundTaskEvent(
			kind,
			taskId,
			instanceId,
			progress,
			string.IsNullOrEmpty(errorMessage) ? null : errorMessage);
	}

	internal static IReadOnlyList<BackgroundTaskRegistrationRecord> ReadStore(string path)
	{
		if (!File.Exists(path))
		{
			return [];
		}

		var file = new FileInfo(path);
		if (file.Length > MaximumStoreLength)
		{
			throw new InvalidDataException("The background task registration store is too large.");
		}

		using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		using var reader = new BinaryReader(stream, Encoding.UTF8);
		if (reader.ReadInt32() != FormatVersion)
		{
			throw new InvalidDataException(
				"The background task registration store has an unsupported format.");
		}

		var count = reader.ReadInt32();
		if (count is < 0 or > MaximumRegistrations)
		{
			throw new InvalidDataException(
				"The background task registration store has an invalid item count.");
		}

		var registrations = new List<BackgroundTaskRegistrationRecord>(count);
		for (var index = 0; index < count; index++)
		{
			var taskIdBytes = reader.ReadBytes(16);
			if (taskIdBytes.Length != 16)
			{
				throw new InvalidDataException(
					"The background task registration store ended unexpectedly.");
			}

			var taskId = new Guid(taskIdBytes);
			var name = reader.ReadString();
			var taskEntryPoint = reader.ReadString();
			var freshnessTime = reader.ReadUInt32();
			if (freshnessTime < TimeTrigger.MinimumFreshnessTime)
			{
				throw new InvalidDataException(
					"The background task registration store has an invalid trigger interval.");
			}

			var oneShot = reader.ReadBoolean();
			var cancelOnConditionLoss = reader.ReadBoolean();
			var isNetworkRequested = reader.ReadBoolean();
			var groupId = ReadNullableString(reader);
			var groupName = ReadNullableString(reader);
			var executablePath = reader.ReadString();
			var workingDirectory = reader.ReadString();
			var argumentCount = reader.ReadInt32();
			if (argumentCount is < 0 or > MaximumArguments)
			{
				throw new InvalidDataException(
					"The background task registration store has an invalid argument count.");
			}

			var arguments = new string[argumentCount];
			for (var argumentIndex = 0; argumentIndex < argumentCount; argumentIndex++)
			{
				arguments[argumentIndex] = reader.ReadString();
			}

			registrations.Add(new BackgroundTaskRegistrationRecord
			{
				TaskId = taskId,
				Name = name,
				TaskEntryPoint = taskEntryPoint,
				Trigger = new TimeTrigger(freshnessTime, oneShot),
				CancelOnConditionLoss = cancelOnConditionLoss,
				IsNetworkRequested = isNetworkRequested,
				GroupId = groupId,
				GroupName = groupName,
				ExecutablePath = executablePath,
				ExecutableArguments = arguments,
				WorkingDirectory = workingDirectory
			});
		}

		if (stream.Position != stream.Length)
		{
			throw new InvalidDataException(
				"The background task registration store contains trailing data.");
		}

		return registrations;
	}

	internal static void WriteStore(
		string path,
		IEnumerable<BackgroundTaskRegistrationRecord> registrations)
	{
		var registrationList = registrations.ToList();
		if (registrationList.Count > MaximumRegistrations)
		{
			throw new InvalidOperationException(
				$"No more than {MaximumRegistrations} background tasks can be registered.");
		}

		var directory = Path.GetDirectoryName(path)
			?? throw new InvalidOperationException(
				"The background task registration path has no parent directory.");
		Directory.CreateDirectory(directory);
		var temporaryPath = path + ".tmp";
		try
		{
			using (var stream = new FileStream(
				temporaryPath,
				FileMode.Create,
				FileAccess.Write,
				FileShare.None,
				81920,
				FileOptions.WriteThrough))
			using (var writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
			{
				writer.Write(FormatVersion);
				writer.Write(registrationList.Count);
				foreach (var registration in registrationList)
				{
					writer.Write(registration.TaskId.ToByteArray());
					writer.Write(registration.Name);
					writer.Write(registration.TaskEntryPoint);
					writer.Write(registration.Trigger.FreshnessTime);
					writer.Write(registration.Trigger.OneShot);
					writer.Write(registration.CancelOnConditionLoss);
					writer.Write(registration.IsNetworkRequested);
					WriteNullableString(writer, registration.GroupId);
					WriteNullableString(writer, registration.GroupName);
					writer.Write(registration.ExecutablePath);
					writer.Write(registration.WorkingDirectory);
					writer.Write(registration.ExecutableArguments.Count);
					foreach (var argument in registration.ExecutableArguments)
					{
						writer.Write(argument);
					}
				}

				writer.Flush();
				stream.Flush(flushToDisk: true);
			}

			File.Move(temporaryPath, path, overwrite: true);
		}
		finally
		{
			if (File.Exists(temporaryPath))
			{
				File.Delete(temporaryPath);
			}
		}
	}

	private static FileStream AcquireFileLock()
	{
		Directory.CreateDirectory(StorageDirectory);
		for (var attempt = 0; ; attempt++)
		{
			try
			{
				return new FileStream(
					LockPath,
					FileMode.OpenOrCreate,
					FileAccess.ReadWrite,
					FileShare.None);
			}
			catch (IOException) when (attempt < 100)
			{
				Thread.Sleep(50);
			}
		}
	}

	private static string? ReadNullableString(BinaryReader reader)
		=> reader.ReadBoolean() ? reader.ReadString() : null;

	private static Guid ReadGuid(BinaryReader reader)
	{
		var bytes = reader.ReadBytes(16);
		if (bytes.Length != 16)
		{
			throw new InvalidDataException(
				"The background task data ended unexpectedly.");
		}

		return new Guid(bytes);
	}

	private static void WriteNullableString(BinaryWriter writer, string? value)
	{
		writer.Write(value is not null);
		if (value is not null)
		{
			writer.Write(value);
		}
	}

	private static bool IsSchedulerFailure(Exception error)
		=> error is IOException
			or UnauthorizedAccessException
			or InvalidOperationException
			or global::System.ComponentModel.Win32Exception;

	private static void TrimEvents(Guid taskId)
	{
		var staleEvents = Directory
			.EnumerateFiles(EventsDirectory, $"{taskId:N}-*.event")
			.OrderByDescending(path => path, StringComparer.Ordinal)
			.Skip(100);
		foreach (var staleEvent in staleEvents)
		{
			try
			{
				File.Delete(staleEvent);
			}
			catch (Exception error) when (
				error is IOException or UnauthorizedAccessException)
			{
				// Retention is best effort: a stale event that cannot be removed right now must
				// not fail the task that reported progress. A later write retries the trim.
				if (typeof(BackgroundTaskRegistrationStore).Log().IsEnabled(LogLevel.Debug))
				{
					typeof(BackgroundTaskRegistrationStore).Log().Debug(
						$"Stale background task event '{staleEvent}' could not be removed: {error}");
				}
			}
		}
	}
}

internal enum BackgroundTaskEventKind : byte
{
	Progress,
	Completed
}

internal sealed record BackgroundTaskEvent(
	BackgroundTaskEventKind Kind,
	Guid TaskId,
	Guid InstanceId,
	uint Progress,
	string? ErrorMessage);
