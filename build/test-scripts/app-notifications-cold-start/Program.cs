#nullable enable

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Microsoft.Windows.AppNotifications;

namespace Uno.AppNotifications.ColdStartTests;

internal static class Program
{
	private const string AppIdPrefix = "Uno.AppNotifications.ColdStartTests.";
	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

	[STAThread]
	private static int Main(string[] args)
	{
		try
		{
			if (!OperatingSystem.IsWindows())
			{
				throw new PlatformNotSupportedException("This process-level test requires Windows and Windows App Runtime 2.3.");
			}

			var executable = Environment.ProcessPath ?? throw new InvalidOperationException("An apphost is required.");
			if (!string.Equals(Path.GetFileNameWithoutExtension(executable), "AppNotificationsColdStart", StringComparison.OrdinalIgnoreCase))
			{
				throw new InvalidOperationException("Run the generated AppNotificationsColdStart.exe, not dotnet.exe.");
			}

			if (args.Contains("--child") || args.Contains("--cleanup"))
			{
				var appIdIndex = Array.IndexOf(args, "--app-id");
				var appId = appIdIndex >= 0 && appIdIndex + 1 < args.Length ? args[appIdIndex + 1] : string.Empty;
				if (!appId.StartsWith(AppIdPrefix, StringComparison.Ordinal) ||
					!Guid.TryParseExact(appId.AsSpan(AppIdPrefix.Length), "N", out _))
				{
					throw new InvalidOperationException("A unique test app identity is required.");
				}
				RunChild(appId, args.Contains("--custom-registration"), args.Contains("--cleanup"), args.Contains("--expect-startup-timeout"));
			}
			else
			{
				RunProcesses(executable);
			}
			return 0;
		}
		catch (Exception exception)
		{
			Console.Error.WriteLine(exception);
			return 1;
		}
	}

	[SupportedOSPlatform("windows")]
	private static void RunProcesses(string executable)
	{
		foreach (var scenario in new[]
		{
			(Name: "ordinary", Argument: (string?)null),
			(Name: "unrelated", Argument: "--other-activation=background"),
			// Neither push manager nor a push payload is initialized: toast registration must not decode these.
			(Name: "push-before-registration", Argument: "----WindowsAppRuntimePushServer:"),
			(Name: "encoded-push-before-registration", Argument: "----ms-protocol:ms-encodedlaunch://App?ContractId=Windows.Push"),
			(Name: "cold-toast", Argument: "----AppNotificationActivated:"),
			(Name: "cold-toast-timeout-retry", Argument: "----AppNotificationActivated:"),
		})
		{
			foreach (var customRegistration in new[] { false, true })
			{
				var appId = AppIdPrefix + Guid.NewGuid().ToString("N");
				var startInfo = CreateChildStartInfo(executable, appId);
				startInfo.ArgumentList.Add("--child");
				if (scenario.Argument is { } argument)
				{
					startInfo.ArgumentList.Add(argument);
				}
				if (customRegistration)
				{
					startInfo.ArgumentList.Add("--custom-registration");
				}
				if (scenario.Name == "cold-toast-timeout-retry")
				{
					startInfo.ArgumentList.Add("--expect-startup-timeout");
				}

				Exception? failure = null;
				try
				{
					RunProcess(startInfo);
				}
				catch (Exception exception)
				{
					failure = exception;
				}
				finally
				{
					try
					{
						if (RegistrationExists(appId))
						{
							var cleanup = CreateChildStartInfo(executable, appId);
							cleanup.ArgumentList.Add("--cleanup");
							RunProcess(cleanup);
						}
						if (RegistrationExists(appId))
						{
							throw new InvalidOperationException($"Test registration was not removed: {appId}");
						}
					}
					catch (Exception exception)
					{
						failure = failure is null ? exception : new AggregateException(failure, exception);
					}
				}
				if (failure is not null)
				{
					ExceptionDispatchInfo.Capture(failure).Throw();
				}
				Console.WriteLine($"PASS: scenario={scenario.Name}, customRegistration={customRegistration}");
			}
		}
	}

	private static ProcessStartInfo CreateChildStartInfo(string executable, string appId)
	{
		var startInfo = new ProcessStartInfo(executable)
		{
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
		};
		startInfo.ArgumentList.Add("--app-id");
		startInfo.ArgumentList.Add(appId);
		return startInfo;
	}

	private static void RunProcess(ProcessStartInfo startInfo)
	{
		using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start the activation test.");
		var output = process.StandardOutput.ReadToEndAsync();
		var error = process.StandardError.ReadToEndAsync();
		if (!process.WaitForExit(60_000))
		{
			process.Kill(entireProcessTree: true);
			process.WaitForExit();
			throw new TimeoutException("The app-notification child process did not finish.");
		}
		Console.Write(output.GetAwaiter().GetResult());
		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"Activation test failed: {error.GetAwaiter().GetResult()}");
		}
	}

	[SupportedOSPlatform("windows")]
	private static void RunChild(string appId, bool customRegistration, bool cleanupOnly, bool expectStartupTimeout)
	{
		uint packageNameLength = 0;
		if (GetCurrentPackageFullName(ref packageNameLength, IntPtr.Zero) != 15700 /* APPMODEL_ERROR_NO_PACKAGE */)
		{
			throw new InvalidOperationException("This test must not run under a package's shared notification identity.");
		}
		Marshal.ThrowExceptionForHR(OleInitialize(IntPtr.Zero));
		try
		{
			// An explicit ID avoids the SDK's persistent executable-path-to-app-ID mapping.
			Marshal.ThrowExceptionForHR(SetCurrentProcessExplicitAppUserModelID(appId));
			Console.WriteLine($"Test app identity: {appId}");
			if (cleanupOnly && !RegistrationExists(appId))
			{
				return;
			}
			var (backendType, backend) = GetBackend();
			if (cleanupOnly)
			{
				backendType.GetMethod("UnregisterAll")!.Invoke(backend, null);
				return;
			}

			// Use the production backend with the manager's in-memory test constructor to avoid application-state files.
			var constructor = typeof(AppNotificationManager).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
				.Single(candidate => candidate.GetParameters() is { Length: 1 } parameters &&
					parameters[0].ParameterType.Name == "IAppNotificationManagerBackend");
			var manager = (AppNotificationManager)constructor.Invoke(new[] { backend });
			var received = new ConcurrentQueue<AppNotificationActivatedEventArgs>();
			using var invoked = new SemaphoreSlim(0);
			manager.NotificationInvoked += (_, activation) =>
			{
				received.Enqueue(activation);
				invoked.Release();
			};

			void Register()
			{
				if (customRegistration)
				{
					manager.Register("Uno notification activation test", new Uri(Path.Combine(AppContext.BaseDirectory, "uno.png")));
				}
				else
				{
					manager.Register();
				}
			}

			const string initialArgument = "action=open&value=cold%20start";
			var registered = backendType.GetField("_isRegistered", BindingFlags.Instance | BindingFlags.NonPublic)!;
			Exception? failure = null;
			try
			{
				if (expectStartupTimeout)
				{
					try
					{
						Register();
						throw new InvalidOperationException("The native startup decoder did not time out without an activation.");
					}
					catch (TimeoutException)
					{
					}
					if (registered.GetValue(backend) is not false)
					{
						throw new InvalidOperationException("Failed startup reading retained the native foreground registration.");
					}
					using var persistentRegistration = Registry.CurrentUser.OpenSubKey(@"Software\Classes\AppUserModelId\" + appId);
					var activator = persistentRegistration?.GetValue("CustomActivator") as string;
					if (string.IsNullOrEmpty(activator))
					{
						throw new InvalidOperationException("Startup failure incorrectly removed persistent notification registration.");
					}
					using var server = Registry.CurrentUser.OpenSubKey(@"Software\Classes\CLSID\" + activator + @"\LocalServer32");
					if (server is null)
					{
						throw new InvalidOperationException("Startup failure incorrectly removed the persistent COM server.");
					}
				}

				var delivery = Task.Run(() =>
				{
					if (!SpinWait.SpinUntil(() => registered.GetValue(backend) is true, Timeout))
					{
						throw new TimeoutException("The native backend did not register.");
					}
					Activate(appId, initialArgument, "initial input");
				});
				Register();
				delivery.WaitAsync(Timeout).GetAwaiter().GetResult();
				AssertActivation(invoked, received, initialArgument, "initial input");

				manager.Unregister();
				Register();
				if (!received.IsEmpty || invoked.Wait(0))
				{
					throw new InvalidOperationException("Re-registering replayed the startup activation.");
				}

				Activate(appId, "action=foreground", "subsequent input");
				AssertActivation(invoked, received, "action=foreground", "subsequent input");
				if (!received.IsEmpty || invoked.Wait(0))
				{
					throw new InvalidOperationException("A notification activation was delivered more than once.");
				}
			}
			catch (Exception exception)
			{
				failure = exception;
			}
			finally
			{
				try
				{
					manager.UnregisterAll();
				}
				catch (Exception exception)
				{
					failure = failure is null ? exception : new AggregateException(failure, exception);
				}
			}
			if (failure is not null)
			{
				ExceptionDispatchInfo.Capture(failure).Throw();
			}
		}
		finally
		{
			OleUninitialize();
		}
	}

	[SupportedOSPlatform("windows")]
	private static bool RegistrationExists(string appId)
	{
		using var registration = Registry.CurrentUser.OpenSubKey(@"Software\Classes\AppUserModelId\" + appId);
		return registration is not null;
	}

	private static (Type Type, object Instance) GetBackend()
	{
		var backendType = Assembly.Load("Uno.UI.Runtime.Skia.Win32")
			.GetType("Uno.UI.Runtime.Skia.Win32.Win32AppNotificationManagerBackend", throwOnError: true)!;
		var backend = backendType.GetProperty("Instance")!.GetValue(null)!;
		if (backendType.GetProperty("IsSupported")!.GetValue(backend) is not true)
		{
			throw new InvalidOperationException("The Windows App SDK notification runtime is unavailable. Run unelevated with Windows App Runtime 2.3 installed.");
		}
		return (backendType, backend);
	}

	private static void AssertActivation(
		SemaphoreSlim invoked,
		ConcurrentQueue<AppNotificationActivatedEventArgs> received,
		string argument,
		string input)
	{
		if (!invoked.Wait(Timeout) || !received.TryDequeue(out var activation) ||
			activation.Argument != argument || !activation.UserInput.TryGetValue("reply", out var value) || value != input)
		{
			throw new InvalidOperationException("The native activation or its user input was not forwarded.");
		}
	}

	[SupportedOSPlatform("windows")]
	private static void Activate(string appId, string argument, string input)
	{
		var nativeType = Assembly.Load("Microsoft.Windows.AppNotifications.Projection")
			.GetType("Microsoft.Windows.AppNotifications.AppNotificationManager", throwOnError: true)!;
		var nativeManager = nativeType.GetProperty("Default")!.GetValue(null)!;
		var winrtObject = nativeType.GetInterface("WinRT.IWinRTObject")!;
		var reference = winrtObject.GetProperty("NativeObject")!.GetValue(nativeManager)!;
		var pointer = (IntPtr)reference.GetType().GetProperty("ThisPtr")!.GetValue(reference)!;
		var callbackId = new Guid("53E31837-6600-4A81-9395-75CFFE746F94");
		Marshal.ThrowExceptionForHR(Marshal.QueryInterface(pointer, in callbackId, out var callback));
		try
		{
			var vtable = Marshal.ReadIntPtr(callback);
			var activate = Marshal.GetDelegateForFunctionPointer<ActivationCallback>(Marshal.ReadIntPtr(vtable, 3 * IntPtr.Size));
			var inputs = new[] { new NotificationInput { Key = "reply", Value = input } };
			Marshal.ThrowExceptionForHR(activate(callback, appId, argument, inputs, (uint)inputs.Length));
		}
		finally
		{
			Marshal.Release(callback);
		}
		GC.KeepAlive(nativeManager);
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	private struct NotificationInput
	{
		[MarshalAs(UnmanagedType.LPWStr)]
		public string Key;

		[MarshalAs(UnmanagedType.LPWStr)]
		public string Value;
	}

	[UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
	private delegate int ActivationCallback(
		IntPtr instance,
		[MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
		[MarshalAs(UnmanagedType.LPWStr)] string invokedArgs,
		[In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4)] NotificationInput[] data,
		uint count);

	[DllImport("ole32.dll", ExactSpelling = true)]
	private static extern int OleInitialize(IntPtr reserved);

	[DllImport("ole32.dll", ExactSpelling = true)]
	private static extern void OleUninitialize();

	[DllImport("kernel32.dll", ExactSpelling = true)]
	private static extern int GetCurrentPackageFullName(ref uint packageFullNameLength, IntPtr packageFullName);

	[DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
	private static extern int SetCurrentProcessExplicitAppUserModelID(string appId);
}
