#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.RemoteControl.VS.Helpers
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
			p.OutputDataReceived += (s, e) => { if (e.Data != null) { output.Append(e.Data); } };
			p.ErrorDataReceived += (s, e) => { if (e.Data != null) { error.Append(e.Data); } };

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
