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
using Microsoft.Build.Execution;
using Uno.Logging;
using Uno.UI.SourceGenerators.Telemetry;

namespace Uno.UI.SourceGenerators.XamlGenerator
{
	internal partial class XamlCodeGeneration
	{
		private Telemetry.Telemetry _telemetry;

		public bool IsRunningCI =>
			Environment.GetEnvironmentVariable("TF_BUILD").HasValue() // https://docs.microsoft.com/en-us/azure/devops/pipelines/build/variables?tabs=yaml&view=azure-devops#system-variables
			|| Environment.GetEnvironmentVariable("TRAVIS").HasValue() // https://docs.travis-ci.com/user/environment-variables/#default-environment-variables
			|| Environment.GetEnvironmentVariable("JENKINS_URL").HasValue() // https://wiki.jenkins.io/display/JENKINS/Building+a+software+project#Buildingasoftwareproject-belowJenkinsSetEnvironmentVariables
			|| Environment.GetEnvironmentVariable("APPVEYOR").HasValue(); // https://www.appveyor.com/docs/environment-variables/

		private void InitTelemetry(ProjectInstance msbProject)
		{
			var telemetryOptOut = msbProject.GetProperty("UnoPlatformTelemetryOptOut")?.EvaluatedValue ?? "";

			bool? isTelemetryOptout()
				=> telemetryOptOut.Equals("true", StringComparison.OrdinalIgnoreCase)
				|| telemetryOptOut.Equals("1", StringComparison.OrdinalIgnoreCase);

			_telemetry = new Telemetry.Telemetry(isTelemetryOptout);

			if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Information))
			{
				this.Log().InfoFormat($"Telemetry enabled: {_telemetry.Enabled}");
			}
		}

		private bool IsTelemetryEnabled => _telemetry?.Enabled ?? false;

		private void TrackGenerationDone(TimeSpan elapsed)
		{
			if (IsTelemetryEnabled)
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
				catch (Exception e)
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Telemetry failure: {e}");
					}
				}
			}
		}

		private void TrackGenerationFailed(Exception exception, TimeSpan elapsed)
		{
			if (IsTelemetryEnabled)
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
				catch (Exception telemetryException)
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Telemetry failure: {telemetryException}");
					}
				}
			}
		}

		private void TrackStartGeneration(XamlFileDefinition[] files)
		{
			if (IsTelemetryEnabled)
			{
				try
				{
					// Determine if the Uno.UI solution is built
					var isBuildingUno = _projectInstance.GetProperty("MSBuildProjectName")?.EvaluatedValue == "Uno.UI";

					_telemetry.TrackEvent(
						"generate-xaml",
						new[] {
							("UnoXaml", XamlRedirection.XamlConfig.IsUnoXaml.ToString()),
							("IsWasm", _isWasm.ToString()),
							("IsDebug", _isDebug.ToString()),
							("TargetFramework", _projectInstance.GetProperty("TargetFramework")?.EvaluatedValue.ToString()),
							("UnoRuntime", BuildUnoRuntimeValue()),
							("IsBuildingUnoSolution", isBuildingUno.ToString()),
							("IsUiAutomationMappingEnabled", _isUiAutomationMappingEnabled.ToString()),
							("DefaultLanguage", _defaultLanguage ?? "Unknown"),
							("IsRunningCI", IsRunningCI.ToString()),
						},
						new[] { ("FileCount", (double)files.Length) }
					);
				}
				catch (Exception telemetryException)
				{
					if (this.Log().IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						this.Log().Debug($"Telemetry failure: {telemetryException}");
					}
				}
			}
		}

		private string BuildUnoRuntimeValue()
		{
			var constants = _projectInstance.GetProperty("DefineConstants").EvaluatedValue;

			if (constants != null)
			{
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
			}

			return "Unknown";
		}
	}
}
