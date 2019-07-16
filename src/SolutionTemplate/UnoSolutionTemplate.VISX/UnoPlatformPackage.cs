using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Task = System.Threading.Tasks.Task;

namespace UnoSolutionTemplate
{
	/// <summary>
	/// This is the class that implements the package exposed by this assembly.
	/// </summary>
	/// <remarks>
	/// <para>
	/// The minimum requirement for a class to be considered a valid package for Visual Studio
	/// is to implement the IVsPackage interface and register itself with the shell.
	/// This package uses the helper classes defined inside the Managed Package Framework (MPF)
	/// to do it: it derives from the Package class that provides the implementation of the
	/// IVsPackage interface and uses the registration attributes defined in the framework to
	/// register itself and its components with the shell. These attributes tell the pkgdef creation
	/// utility what data to put into .pkgdef file.
	/// </para>
	/// <para>
	/// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
	/// </para>
	/// </remarks>
	[PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
	[Guid(UnoPlatformPackage.PackageGuidString)]
	[ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	public sealed class UnoPlatformPackage : AsyncPackage
	{
		private DTE2 _dte;
		private _dispSolutionEvents_OpenedEventHandler _openedHandler;

		/// <summary>
		/// UnoPlatformPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "e2245c5b-bbe5-40c8-96d6-94ea655a5ff7";

		/// <summary>
		/// Initializes a new instance of the <see cref="UnoPlatformPackage"/> class.
		/// </summary>
		public UnoPlatformPackage()
		{
			// Inside this method you can place any initialization code that does not require
			// any Visual Studio service because at this point the package object is created but
			// not sited yet inside Visual Studio environment. The place to do all the other
			// initialization is the Initialize method.
		}

		#region Package Members

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			_dte = await GetServiceAsync(typeof(DTE)) as DTE2;

			_openedHandler = () => OnOpened();

			_dte.Events.SolutionEvents.Opened += _openedHandler;
		}

		#endregion

		private async void OnOpened()
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await this.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);

			do
			{
				try
				{
					var componentModel = (IComponentModel)GetService(typeof(SComponentModel));
					IVsPackageInstallerServices installerServices = componentModel.GetService<IVsPackageInstallerServices>();
					var installedPackages = installerServices.GetInstalledPackages();

					var umbrella = installedPackages
						.Where(p => p.Id.Equals("Umbrella", StringComparison.OrdinalIgnoreCase))
						.OrderByDescending(p => p.VersionString)
						.LastOrDefault();

					if (umbrella != null)
					{
						if (string.IsNullOrWhiteSpace(umbrella.InstallPath.Trim()))
						{
							_infoAction("The umbrella package has not been restored yet, retrying...");
						}
						else
						{
							var toolsPath = System.IO.Path.Combine(umbrella.InstallPath, "tools");
							var asmPath = System.IO.Path.Combine(toolsPath, "umbrella.powershell.dll");
							var asm = System.Reflection.Assembly.LoadFrom(asmPath);

							var dispatcherType = asm.GetType("Umbrella.Powershell.DomainDispatcher");

							if (dispatcherType.GetConstructor(new[] { typeof(DTE2), typeof(string), typeof(CommandBarPopup) }) != null)
							{
								_dispatcher = Activator.CreateInstance(dispatcherType, dte, toolsPath, mainMenu) as IDisposable;
							}
							else
							{
								_infoAction("The loaded solution contains an Umbrella package that does not provide UI commands.");
							}
						}
					}

					CreateUmbrellaIntegrationCommandsMenu(dte, mainMenu);

					return;
				}
				catch (Exception e)
				{
					_errorAction(e);
				}

				await System.Threading.Tasks.Task.Delay(5000);
			}
			while (true);
		}
	}
}
