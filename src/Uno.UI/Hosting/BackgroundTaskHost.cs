#nullable enable

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace Uno.UI.Hosting;

internal sealed class BackgroundTaskHost : UnoPlatformHost
{
	private readonly BackgroundTaskActivationInfo _activation;

	internal BackgroundTaskHost(BackgroundTaskActivationInfo activation)
		=> _activation = activation;

	[UnconditionalSuppressMessage(
		"Trimming",
		"IL2026",
		Justification = "Background task entry points are reflection-activated and must be preserved by the application.")]
	protected override void Initialize()
	{
		var applicationAssembly = AppDomain.CurrentDomain
			.GetAssemblies()
			.FirstOrDefault(assembly => string.Equals(
				assembly.GetName().Name,
				_activation.ApplicationAssemblyName,
				StringComparison.Ordinal));
		if (applicationAssembly is null &&
			_activation.ApplicationAssemblyPath is { Length: > 0 } assemblyPath &&
			File.Exists(assemblyPath))
		{
			applicationAssembly = Assembly.LoadFrom(assemblyPath);
		}

		if (applicationAssembly is null)
		{
			try
			{
				applicationAssembly = Assembly.Load(
					new AssemblyName(_activation.ApplicationAssemblyName));
			}
			catch (FileNotFoundException)
			{
			}
		}

		if (applicationAssembly is null)
		{
			throw new InvalidOperationException(
				$"The application assembly '{_activation.ApplicationAssemblyName}' "
				+ "could not be loaded for background activation.");
		}

		Windows.ApplicationModel.Package.SetEntryAssembly(applicationAssembly);
		Environment.ExitCode = BackgroundTaskActivation.Run(_activation.TaskId);
	}

	protected override Task RunLoop()
		=> Task.CompletedTask;
}
