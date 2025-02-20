using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Uno.Extensions;

namespace Uno.UI.RemoteControl.Helpers;

public class AssemblyHelper
{
	private static readonly ILogger _log = typeof(AssemblyHelper).Log();

	public static IImmutableList<Assembly> Load(IImmutableList<string> dllFiles, bool throwIfLoadFailed = false)
	{
		var assemblies = ImmutableList.CreateBuilder<Assembly>();
		foreach (var dll in dllFiles.Distinct(StringComparer.OrdinalIgnoreCase))
		{
			try
			{
				_log.Log(LogLevel.Debug, $"Loading add-in assembly '{dll}'.");

				assemblies.Add(Assembly.LoadFrom(dll));
			}
			catch (Exception err)
			{
				_log.Log(LogLevel.Error, $"Failed to load assembly '{dll}'.", err);

				if (throwIfLoadFailed)
				{
					throw;
				}
			}
		}

		return assemblies.ToImmutable();
	}
}
