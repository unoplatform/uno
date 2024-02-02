#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Uno.UI.RemoteControl.Server.Processors.Helpers
{
	internal class ProcessHelper
	{
		public static (int exitCode, string output, string error) RunProcess(string executable, string parameters, string? workingDirectory = null)
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				&& executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
			{
				executable = Path.GetFileNameWithoutExtension(executable);
			}

			var p = new Process
			{
				StartInfo =
				{
					UseShellExecute = false,
					RedirectStandardOutput = true,
					RedirectStandardError = true,
					FileName = executable,
					Arguments = parameters
				}
			};

			if (workingDirectory != null)
			{
				p.StartInfo.WorkingDirectory = workingDirectory;
			}

			var output = new StringBuilder();
			var error = new StringBuilder();
			var elapsed = Stopwatch.StartNew();
			p.OutputDataReceived += (s, e) => { if (e.Data != null) { /* Log.LogMessage($"[{elapsed.Elapsed}] {e.Data}");*/ output.AppendLine(e.Data); } };
			p.ErrorDataReceived += (s, e) => { if (e.Data != null) { /*Log.LogError($"[{elapsed.Elapsed}] {e.Data}");*/ error.AppendLine(e.Data); } };

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
				throw new($"Failed to start [{executable}]");
			}
		}
	}
}
