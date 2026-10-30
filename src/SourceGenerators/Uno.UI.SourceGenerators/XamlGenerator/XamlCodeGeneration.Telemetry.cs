using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
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

		private Telemetry _telemetry = null!;

		private void InitTelemetry(GeneratorExecutionContext context)
		{
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

			// Read from the build, not the process: _isDesignTimeBuild isn't set yet, and a reused compiler server
			// may predate the opt-out variable, which the telemetry library also ignores when it's "1".
			_telemetry = CreateTelemetry(
				context.GetMSBuildPropertyValue("UnoPlatformTelemetryOptOut"),
				context.GetMSBuildPropertyValue("UNO_PLATFORM_TELEMETRY_OPTOUT"),
				Helpers.DesignTimeHelper.IsDesignTime(context),
				() => new Telemetry(
					InstrumentationKey,
					"uno/generation",
					currentDirectoryProvider: getCurrentDirectory,
					versionAssembly: GetType().Assembly));
		}

		/// <summary>
		/// Creates no client when the build or its environment opts out, for a design-time build, or when the
		/// generator itself runs in a browser.
		/// </summary>
		internal static T CreateTelemetry<T>(string telemetryOptOut, string environmentOptOut, bool isDesignTimeBuild, Func<T> createClient)
			where T : class
		{
			static bool optsOut(string value)
				=> string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
				|| string.Equals(value, "1", StringComparison.OrdinalIgnoreCase);

			var isOptedOut = optsOut(telemetryOptOut)
				|| optsOut(environmentOptOut)
				|| isDesignTimeBuild
				|| RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));

			return isOptedOut ? null : createClient();
		}

		private bool IsTelemetryEnabled => _telemetry?.Enabled ?? false;

		private void TrackGenerationDone(TimeSpan elapsed)
		{
			if (IsTelemetryEnabled
				&& !_isDesignTimeBuild)
			{
				try
				{
					_telemetry?.TrackEvent(
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
					_telemetry?.TrackEvent(
						"generate-xaml-failed",
						new[] {
							("ExceptionType", exception.GetType().ToString())
						},
						new[] { ("Duration", elapsed.TotalSeconds) }
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

					_telemetry?.TrackEvent(
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
