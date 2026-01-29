using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Xml;
using Uno.Roslyn;
using Microsoft.CodeAnalysis;
using Uno.Extensions;
using Uno.DevTools.Telemetry;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal partial class XamlCodeGeneration
	{
		private const string InstrumentationKey = "9a44058e-1913-4721-a979-9582ab8bedce";

		private Telemetry _telemetry;

		private void InitTelemetry(GeneratorExecutionContext context)
		{
			var telemetryOptOut = context.GetMSBuildPropertyValue("UnoPlatformTelemetryOptOut");

			bool? isTelemetryOptout()
				=> telemetryOptOut.Equals("true", StringComparison.OrdinalIgnoreCase)
				|| telemetryOptOut.Equals("1", StringComparison.OrdinalIgnoreCase)
				|| _isDesignTimeBuild;

			string getCurrentDirectory()
			{
				var solutionDir = context.GetMSBuildPropertyValue("SolutionDir");
				if (!string.IsNullOrEmpty(solutionDir))
				{
					return solutionDir;
				}

				var projectDir = context.GetMSBuildPropertyValue("MSBuildProjectFullPath");
				if (!string.IsNullOrEmpty(projectDir))
				{
					return projectDir;
				}

				return Environment.CurrentDirectory;
			}

			_telemetry = new Telemetry(
				InstrumentationKey,
				"uno/generation",
				enabledProvider: isTelemetryOptout,
				currentDirectoryProvider: getCurrentDirectory,
				versionAssembly: GetType().Assembly);
		}

		private bool IsTelemetryEnabled => _telemetry?.Enabled ?? false;

		private void TrackGenerationDone(TimeSpan elapsed)
		{
			if (IsTelemetryEnabled
				&& !_isDesignTimeBuild)
			{
				try
				{
					_telemetry.TrackEvent(
						"generate-xaml-done",
						[],
						new[] { ("Duration", elapsed.TotalSeconds) }
					);
				}
#pragma warning disable CS0168 // unused parameter
				catch (Exception e)
				{
#if DEBUG
					Console.WriteLine($"Telemetry failure: {e}");
#endif
				}
#pragma warning restore CS0168 // unused parameter
			}
		}

		private void TrackGenerationFailed(Exception exception, TimeSpan elapsed)
		{
			if (IsTelemetryEnabled
				&& !_isDesignTimeBuild)
			{
				try
				{
					var measurements = new Dictionary<string, double>
					{
						["Duration"] = elapsed.TotalSeconds
					};

					_telemetry.TrackException(
						exception,
						properties: null,
						measurements
					);
				}
#pragma warning disable CS0168 // unused parameter
				catch (Exception telemetryException)
				{
#if DEBUG
					Console.WriteLine($"Telemetry failure: {telemetryException}");
#endif
				}
#pragma warning restore CS0168 // unused parameter
			}
		}

		private void TrackStartGeneration(XamlFileDefinition[] files)
		{
			if (IsTelemetryEnabled
				&& !_isDesignTimeBuild)
			{
				try
				{
					// Determine if the Uno.UI solution is built
					var isBuildingUno = _generatorContext.GetMSBuildPropertyValue("MSBuildProjectName") == "Uno.UI";

					_telemetry.TrackEvent(
						"generate-xaml",
						new[] {
							("IsWasm", _isWasm.ToString()),
							("IsDebug", _isDebug.ToString()),
							("TargetFramework",  _generatorContext.GetMSBuildPropertyValue("TargetFramework")?.ToString()),
							("UnoRuntime", BuildUnoRuntimeValue()),
							("IsBuildingUnoSolution", isBuildingUno.ToString()),
							("IsUiAutomationMappingEnabled", _isUiAutomationMappingEnabled.ToString()),
							("DefaultLanguage", _defaultLanguage ?? "Unknown"),
							("BuildingInsideVisualStudio", _generatorContext.GetMSBuildPropertyValue("BuildingInsideVisualStudio")?.ToString().ToLowerInvariant()),
							("IDE", BuildIDEName()),
						},
						new[] { ("FileCount", (double)files.Length) }
					);
				}
#pragma warning disable CS0168 // unused parameter
				catch (Exception telemetryException)
				{
#if DEBUG
					Console.WriteLine($"Telemetry failure: {telemetryException}");
#endif
				}
#pragma warning restore CS0168 // unused parameter
			}
		}

		private string BuildIDEName()
		{
			if (bool.TryParse(_generatorContext.GetMSBuildPropertyValue("BuildingInsideVisualStudio")?.ToString(), out var insideVS) && insideVS)
			{
				return "vswin";
			}
			else if (_generatorContext.GetMSBuildPropertyValue("UnoPlatformIDE")?.ToString() is { } unoPlatformIDE)
			{
				return unoPlatformIDE;
			}
			else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VSCODE_CWD")) || Environment.GetEnvironmentVariable("TERM_PROGRAM") == "vscode")
			{
				return "vscode";
			}
			else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("IDEA_INITIAL_DIRECTORY")))
			{
				return "rider";
			}
			else
			{
				return "unknown";
			}
		}

		private string BuildUnoRuntimeValue()
		{
			var constants = _generatorContext.GetMSBuildPropertyValue("DefineConstantsProperty");

			if (constants.Contains("__WASM__"))
			{
				return "WebAssembly";
			}
			if (constants.Contains("__SKIA__"))
			{
				return "Skia";
			}
			if (constants.Contains("__TIZEN__"))
			{
				return "Tizen";
			}
			if (constants.Contains("UNO_REFERENCE_API"))
			{
				return "Reference";
			}

			return "Unknown";
		}
	}
}
