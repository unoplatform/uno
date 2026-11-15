using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Automation;
using System.Xml.Linq;

internal static class Program
{
	[MTAThread]
	private static int Main(string[] args)
	{
		if (args.Length != 2)
		{
			Console.Error.WriteLine("Usage: Win32UiaEventProbe <SamplesApp.dll> <results.xml>");
			return 1;
		}

		// The UIA client must subscribe from an MTA thread in a separate process from the provider.
		return Run(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]));
	}

	private static int Run(string app, string results)
	{
		var probeDirectory = results + ".probe-" + Guid.NewGuid().ToString("N");
		Directory.CreateDirectory(probeDirectory);
		var logLock = new object();
		var start = new ProcessStartInfo("dotnet")
		{
			WorkingDirectory = Path.GetDirectoryName(app)!,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};
		start.ArgumentList.Add(app);
		start.ArgumentList.Add("--runtime-tests=" + results);
		start.Environment["UNO_UIA_STRUCTURE_PROBE"] = probeDirectory;
		start.Environment["UITEST_RUNTIME_TESTS_FILTER"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(
			"Uno.UI.RuntimeTests.Tests.Windows_UI_Xaml_Automation.Given_Win32ProviderLifetime"));

		using var process = new Process { StartInfo = start };
		void Log(object sender, DataReceivedEventArgs e)
		{
			if (e.Data is not null)
			{
				lock (logLock)
				{
					File.AppendAllText(results + ".log", e.Data + Environment.NewLine);
				}
			}
		}
		process.OutputDataReceived += Log;
		process.ErrorDataReceived += Log;
		process.Start();
		process.BeginOutputReadLine();
		process.BeginErrorReadLine();

		AutomationElement? parent = null;
		int[]? childRuntimeId = null;
		StructureChangedEventHandler handler = (sender, e) =>
		{
			try
			{
				if (e.StructureChangeType == StructureChangeType.ChildAdded &&
					sender is AutomationElement child && child.Current.AutomationId == "UnoUiaEventChild")
				{
					Volatile.Write(ref childRuntimeId, child.GetRuntimeId());
					File.WriteAllText(Path.Join(probeDirectory, "added"), string.Empty);
				}
				else if (e.StructureChangeType == StructureChangeType.ChildRemoved &&
					Volatile.Read(ref childRuntimeId) is { } expected && e.GetRuntimeId().SequenceEqual(expected))
				{
					File.WriteAllText(Path.Join(probeDirectory, "removed"), string.Join(",", expected));
				}
			}
			catch (ElementNotAvailableException)
			{
				// Other tests can retire providers before their queued event reaches this client.
			}
		};

		try
		{
			var deadline = Stopwatch.StartNew();
			while (!process.HasExited && deadline.Elapsed < TimeSpan.FromMinutes(10))
			{
				if (parent is null && File.Exists(Path.Join(probeDirectory, "ready")))
				{
					process.Refresh();
					var window = AutomationElement.FromHandle(process.MainWindowHandle);
					parent = window.FindFirst(TreeScope.Descendants,
						new PropertyCondition(AutomationElement.AutomationIdProperty, "UnoUiaEventParent"));
					if (parent is not null)
					{
						Automation.AddStructureChangedEventHandler(parent, TreeScope.Subtree, handler);
						File.WriteAllText(Path.Join(probeDirectory, "subscribed"), string.Empty);
					}
				}
				Thread.Sleep(50);
			}

			if (!process.HasExited)
			{
				Console.Error.WriteLine("Runtime tests timed out.");
				return 1;
			}
			process.WaitForExit();
			if (process.ExitCode != 0 || !File.Exists(results) || !File.Exists(Path.Join(probeDirectory, "removed")))
			{
				Console.Error.WriteLine($"Native event probe failed (app exit {process.ExitCode}); see {results}.log");
				return 1;
			}

			var cases = XDocument.Load(results).Descendants("test-case").ToArray();
			if (cases.Length == 0 || cases.Any(test => (string?)test.Attribute("result") != "Passed"))
			{
				Console.Error.WriteLine($"Runtime tests failed or skipped; see {results}");
				return 1;
			}
			Console.WriteLine($"{cases.Length} tests passed; native ChildAdded and ChildRemoved matched runtime ID [{string.Join(",", childRuntimeId!)}].");
			return 0;
		}
		finally
		{
			if (parent is not null)
			{
				Automation.RemoveStructureChangedEventHandler(parent, handler);
			}
			if (!process.HasExited)
			{
				process.Kill(entireProcessTree: true);
				process.WaitForExit();
			}
			try
			{
				Directory.Delete(probeDirectory, recursive: true);
			}
			catch (Exception e) when (e is IOException or UnauthorizedAccessException)
			{
				// A leftover marker folder must not fail the run.
			}
		}
	}
}
