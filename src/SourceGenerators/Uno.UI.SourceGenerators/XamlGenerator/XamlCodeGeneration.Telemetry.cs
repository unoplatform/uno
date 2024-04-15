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
using Uno.UI.SourceGenerators.Telemetry;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal partial class XamlCodeGeneration
	{
		private Telemetry.Telemetry _telemetry;

		public bool IsRunningCI =>
			!Environment.GetEnvironmentVariable("TF_BUILD").IsNullOrEmpty() // https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables?tabs=yaml&view=azure-devops#system-variables
			|| !Environment.GetEnvironmentVariable("TRAVIS").IsNullOrEmpty() // https://docs.travis-ci.com/user/environment-variables/#default-environment-variables
			|| !Environment.GetEnvironmentVariable("JENKINS_URL").IsNullOrEmpty() // https://wiki.jenkins.io/display/JENKINS/Building+a+software+project#Buildingasoftwareproject-belowJenkinsSetEnvironmentVariables
			|| !Environment.GetEnvironmentVariable("APPVEYOR").IsNullOrEmpty(); // https://www.appveyor.com/docs/environment-variables/

		private void InitTelemetry(GeneratorExecutionContext context)
		{
			var telemetryOptOut = context.GetMSBuildPropertyValue("UnoPlatformTelemetryOptOut");

			bool? isTelemetryOptout()
				=> telemetryOptOut.Equals("true", StringComparison.OrdinalIgnoreCase)
				|| telemetryOptOut.Equals("1", StringComparison.OrdinalIgnoreCase)
				|| _isDesignTimeBuild;

			_telemetry = new Telemetry.Telemetry(isTelemetryOptout);

#if DEBUG
			Console.WriteLine($"Telemetry enabled: {_telemetry.Enabled}");
#endif
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
						new[] {
							("IsRunningCI", IsRunningCI.ToString()),
						},
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
					_telemetry.TrackEvent(
						"generate-xaml-failed",
						new[] {
							("ExceptionType", exception.GetType().ToString()),
							("IsRunningCI", IsRunningCI.ToString()),
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
							("IsRunningCI", IsRunningCI.ToString()),
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
