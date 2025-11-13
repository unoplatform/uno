#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.DevServer.Cli.Helpers
{
	internal class ProcessHelpers
	{
		public static (int exitCode, string output, string error) RunProcess(string executable, string parameters, string? workingDirectory = null)
		{
			var p = new Process
			{
				StartInfo =
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					FileName = executable,
					Arguments = parameters,
					CreateNoWindow = true,
					WindowStyle = ProcessWindowStyle.Hidden,
				}
			};

			if (workingDirectory != null)
			{
				p.StartInfo.WorkingDirectory = workingDirectory;
			}

			var output = new StringBuilder();
			var error = new StringBuilder();
			var elapsed = Stopwatch.StartNew();
			p.OutputDataReceived += (s, e) => { if (e.Data != null) { output.AppendLine(e.Data); } };
			p.ErrorDataReceived += (s, e) => { if (e.Data != null) { error.AppendLine(e.Data); } };

			if (p.Start())
			{
				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				p.WaitForExit();
				var exitCore = p.ExitCode;
				p.Close();

				return (exitCore, output.ToString(), error.ToString());
			}
			else
			{
				throw new Exception($"Failed to start [{executable}]");
			}
		}

		/// <summary>
		/// Attempts to gracefully terminate a process by sending Ctrl+C signal, with fallback to Kill
		/// </summary>
		/// <param name="processId">The process ID to terminate</param>
		/// <param name="timeoutMs">How long to wait for graceful shutdown before using Kill</param>
		/// <returns>True if the process was successfully terminated</returns>
		public static async Task<bool> TryTerminateProcessAsync(int processId, int timeoutMs = 5000)
		{
			try
			{
				var process = Process.GetProcessById(processId);
				if (process.HasExited)
				{
					return true;
				}

				// Try graceful shutdown first
				if (TrySendCtrlC(processId))
				{
					// Wait for graceful shutdown
					var waitTask = Task.Run(() =>
					{
						while (!process.HasExited && timeoutMs > 0)
						{
							Task.Delay(100).Wait();
							timeoutMs -= 100;
						}
					});

					await waitTask;

					if (process.HasExited)
					{
						return true;
					}
				}

				// Fallback to Kill if graceful shutdown failed
				if (!process.HasExited)
				{
					process.Kill();
					await Task.Delay(1000); // Give it a moment to clean up
					return process.HasExited;
				}

				return true;
			}
			catch (ArgumentException)
			{
				// Process doesn't exist anymore
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>
		/// Attempts to send Ctrl+C signal to a process (Windows only)
		/// </summary>
		/// <param name="processId">The process ID</param>
		/// <returns>True if the signal was sent successfully</returns>
		private static bool TrySendCtrlC(int processId)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				return TrySendCtrlCWindows(processId);
			}
			else
			{
				return TrySendSigTermUnix(processId);
			}
		}

		/// <summary>
		/// Sends Ctrl+C to a Windows process
		/// </summary>
		private static bool TrySendCtrlCWindows(int processId)
		{
			try
			{
				// First try to use GenerateConsoleCtrlEvent API
				if (TryGenerateConsoleCtrlEvent(processId))
				{
					return true;
				}

				// Fallback: Try to close the main window gracefully
				return TryCloseMainWindow(processId);
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Attempts to send Ctrl+C using Windows Console API
		/// </summary>
		private static bool TryGenerateConsoleCtrlEvent(int processId)
		{
			try
			{
				// Import Windows API functions
				const uint CTRL_C_EVENT = 0;

				// Try to attach to the target process console
				if (AttachConsole((uint)processId))
				{
					// Disable Ctrl+C handling in our own process temporarily
					SetConsoleCtrlHandler(null, true);

					try
					{
						// Send Ctrl+C event to the console
						bool success = GenerateConsoleCtrlEvent(CTRL_C_EVENT, 0);
						return success;
					}
					finally
					{
						// Re-enable Ctrl+C handling in our process
						SetConsoleCtrlHandler(null, false);
						// Detach from the target console
						FreeConsole();
					}
				}

				return false;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Attempts to close the main window gracefully
		/// </summary>
		private static bool TryCloseMainWindow(int processId)
		{
			try
			{
				var process = Process.GetProcessById(processId);
				if (process.MainWindowHandle != IntPtr.Zero)
				{
					// Send WM_CLOSE message to the main window
					const int WM_CLOSE = 0x0010;
					SendMessage(process.MainWindowHandle, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
					return true;
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Sends SIGTERM to a Unix process
		/// </summary>
		private static bool TrySendSigTermUnix(int processId)
		{
			try
			{
				// Send SIGTERM signal
				var result = RunProcess("kill", $"-TERM {processId}");
				return result.exitCode == 0;
			}
			catch
			{
				return false;
			}
		}

		// Windows API imports for console control
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool AttachConsole(uint dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool FreeConsole();

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool GenerateConsoleCtrlEvent(uint dwCtrlEvent, uint dwProcessGroupId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate? HandlerRoutine, bool Add);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

		// Delegate for console control handler
		private delegate bool ConsoleCtrlDelegate(uint CtrlType);
	}
}
