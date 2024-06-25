using Microsoft.Build.Framework;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Xml;
using Uno.SourceGeneratorTasks.Helpers;
using Microsoft.Build.Execution;
using MSB = Microsoft.Build;
using MSBE = Microsoft.Build.Execution;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.Linq;
using System.Collections.Concurrent;
using Uno.Extensions;
using Uno.Logging;
using Uno.UWPSyncGenerator;

namespace Uno.SourceGeneration.Host
{
	public class ProjectLoader
	{
		private static readonly Microsoft.Extensions.Logging.ILogger _log = typeof(ProjectLoader).Log();

		private static ConcurrentDictionary<Tuple<string, string>, ProjectDetails> _allProjects = new ConcurrentDictionary<Tuple<string, string>, ProjectDetails>();


		public static ProjectDetails LoadProjectDetails(string projectFile, string configuration)
		{
			var key = Tuple.Create(projectFile, configuration);
			ProjectDetails details;

			if (_allProjects.TryGetValue(key, out details))
			{
				if (!details.HasChanged())
				{
					if (_log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						_log.Debug($"Using cached project file details for [{projectFile}]");
					}

					return details;
				}
				else
				{
					if (_log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
					{
						_log.Debug($"Reloading project file details [{projectFile}] as one of its imports has been modified.");
					}
				}
			}

			if (_log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Debug))
			{
				_log.Debug($"Loading project file [{projectFile}]");
			}

			details = new ProjectDetails();

			var properties = new Dictionary<string, string>(ImmutableDictionary<string, string>.Empty);

			properties["DesignTimeBuild"] = "true"; // this will tell msbuild to not build the dependent projects
			properties["BuildingInsideVisualStudio"] = "true"; // this will force CoreCompile task to execute even if all inputs and outputs are up to date
			properties["BuildingInsideUnoSourceGenerator"] = "true"; // this will force prevent the task to run recursively
			properties["Configuration"] = configuration;
			properties["UseHostCompilerIfAvailable"] = "true";
			properties["UseSharedCompilation"] = "true";
			properties["LangVersion"] = Generator.CSharpLangVersion;

			// Platform is intentionally kept as not defined, to avoid having 
			// dependent projects being loaded with a platform they don't support.
			// properties["Platform"] = _platform;

			var xmlReader = XmlReader.Create(projectFile);
			var collection = new Microsoft.Build.Evaluation.ProjectCollection();

			collection.RegisterLogger(new Microsoft.Build.Logging.ConsoleLogger() { Verbosity = LoggerVerbosity.Normal });

			collection.OnlyLogCriticalEvents = false;
			var xml = Microsoft.Build.Construction.ProjectRootElement.Create(xmlReader, collection);

			// When constructing a project from an XmlReader, MSBuild cannot determine the project file path.  Setting the
			// path explicitly is necessary so that the reserved properties like $(MSBuildProjectDirectory) will work.
			xml.FullPath = Path.GetFullPath(projectFile);

			var loadedProject = new Microsoft.Build.Evaluation.Project(
				xml,
				properties,
				toolsVersion: null,
				projectCollection: collection
			);

			var buildTargets = new BuildTargets(loadedProject, "Compile");

			// don't execute anything after CoreCompile target, since we've
			// already done everything we need to compute compiler inputs by then.
			buildTargets.RemoveAfter("CoreCompile", includeTargetInRemoval: false);

			details.Configuration = configuration;
			details.LoadedProject = loadedProject;

			// create a project instance to be executed by build engine.
			// The executed project will hold the final model of the project after execution via msbuild.
			details.ExecutedProject = loadedProject.CreateProjectInstance();

			var hostServices = new Microsoft.Build.Execution.HostServices();

			// connect the host "callback" object with the host services, so we get called back with the exact inputs to the compiler task.
			hostServices.RegisterHostObject(loadedProject.FullPath, "CoreCompile", "Csc", hostObject: null);

			var buildParameters = new Microsoft.Build.Execution.BuildParameters(loadedProject.ProjectCollection);

			// This allows for the loggers to 
			buildParameters.Loggers = collection.Loggers;

			var buildRequestData = new Microsoft.Build.Execution.BuildRequestData(details.ExecutedProject, buildTargets.Targets, hostServices);

			var result = BuildAsync(buildParameters, buildRequestData);

			if (result.Exception == null)
			{
				ValidateOutputPath(details.ExecutedProject);

				var projectFilePath = Path.GetFullPath(Path.GetDirectoryName(projectFile));

				details.References = details.ExecutedProject.GetItems("ReferencePath").Select(r => r.EvaluatedInclude).ToArray();

				if (details.References.None())
				{
					LogFailedTargets(projectFile, result);
					return details;
				}
			}
			else
			{
				LogFailedTargets(projectFile, result);
			}

			_allProjects.TryAdd(key, details);

			details.BuildImportsMap();

			return details;
		}

		private static void LogFailedTargets(string projectFile, BuildResult result)
		{
			if (_log.IsEnabled(Microsoft.Extensions.Logging.LogLevel.Error))
			{
				var failedTargetsEnum = result
					.ResultsByTarget
					.Where(p => p.Value.ResultCode == TargetResultCode.Failure)

					// CoreCompile will most likely fail, particularly if it depends 
					// on generated code to compile.
					.Where(p => p.Key != "CoreCompile")
					.Select(p => p.Value);

				if (failedTargetsEnum.Any())
				{
					var failedTargets = string.Join("; ", failedTargetsEnum);

					_log.Error(
						$"Failed to analyze project file [{projectFile}], the targets [{failedTargets}] failed to execute." +
						"This may be due to an invalid path, such as $(SolutionDir) being used in the csproj; try using relative paths instead." +
						"This may also be related to a missing default configuration directive. Refer to the Uno.SourceGenerator Readme.md file for more details."
					);
				}
			}
		}

		private static void ValidateOutputPath(ProjectInstance project)
		{
			// Ensure that the loaded has an OutputPath defined. This checks for 
			// projects that may have an invalid default configuration|platform, which
			// needs to be fixed first.
			if ((!project.GetProperty("OutputPath")?.EvaluatedValue.HasValue()) ?? false)
			{
				var evaluatedConfig = project.GetProperty("Configuration")?.EvaluatedValue;
				var evaluatedPlatform = project.GetProperty("Platform")?.EvaluatedValue;

				throw new Exception(
					$"The current project does not define an OutputPath property for [{evaluatedConfig}|{evaluatedPlatform}]. " +
					$"Validate that the fallback platform at the top of [{project.FullPath}] matches one of the " +
					$"sections defining an OutputPath property with the [{evaluatedConfig}|{evaluatedPlatform}] condition."
				);
			}
		}

		private static MSBE.BuildResult BuildAsync(MSBE.BuildParameters parameters, MSBE.BuildRequestData requestData)
		{
			var buildManager = MSBE.BuildManager.DefaultBuildManager;

			var taskSource = new TaskCompletionSource<MSB.Execution.BuildResult>();

			buildManager.BeginBuild(parameters);

			// enable cancellation of build
			CancellationTokenRegistration registration = default(CancellationTokenRegistration);

			// execute build async
			try
			{
				buildManager.PendBuildRequest(requestData).ExecuteAsync(sub =>
				{
					// when finished
					try
					{
						var result = sub.BuildResult;
						buildManager.EndBuild();
						registration.Dispose();
						taskSource.TrySetResult(result);
					}
					catch (Exception e)
					{
						taskSource.TrySetException(e);
					}
				}, null);
			}
			catch (Exception e)
			{
				taskSource.SetException(e);
			}

			return taskSource.Task.Result;
		}
	}
}
