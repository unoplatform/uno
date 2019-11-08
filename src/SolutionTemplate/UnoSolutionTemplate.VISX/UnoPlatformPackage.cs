using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
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
using NuGet.VisualStudio;
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
	[ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
	[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	public sealed class UnoPlatformPackage : AsyncPackage
	{
		private Action<string> _warningAction;
		private Action<string> _infoAction;
		private Action<string> _verboseAction;
		private Action<Exception> _errorAction;
		private DTE2 _dte;
		private _dispSolutionEvents_OpenedEventHandler _openedHandler;
		private IDisposable _remoteControl;

		/// <summary>
		/// UnoPlatformPackage GUID string.
		/// </summary>
		public const string PackageGuidString = "e2245c5b-bbe5-40c8-96d6-94ea655a5ff7";

		/// <summary>
		/// A function provider to be called when the remote control plugin as setting global properties.
		/// </summary>
        internal static Func<Task<Dictionary<string, string>>> GlobalFunctionProvider { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnoPlatformPackage"/> class.
        /// </summary>
        public UnoPlatformPackage()
		{
			// Inside this method you can place any initialization code that does not require
			// any Visual Studio service because at this point the package object is created but
			// not sited yet inside Visual Studio environment. The place to do all the other
			// initialization is the Initialize method.

			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
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
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering InitializeAsync for: {0}", this.ToString()));

			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			_dte = await GetServiceAsync(typeof(DTE)) as DTE2;

			_openedHandler = () => InvokeOnMainThread(() => OnOpened());

			_dte.Events.SolutionEvents.Opened += _openedHandler;

			SetupOutputWindow();

			if (_dte.Solution.IsOpen)
			{
				await OnOpened();
			}
		}

		void InvokeOnMainThread(Func<Task> func)
		{
			JoinableTaskFactory.RunAsync(async () =>
			{
				// When initialized asynchronously, the current thread may be a background thread at this point.
				// Do any initialization that requires the UI thread after switching to the UI thread.
				await this.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
				await func();
			});
		}

		private void SetupOutputWindow()
		{
			OutputWindowPane owP = null;

			Func<OutputWindowPane> pane = () =>
			{
				if (owP == null)
				{
					OutputWindow ow = _dte.ToolWindows.OutputWindow;
						// Add a new pane to the Output window.
						owP = ow
						.OutputWindowPanes
						.OfType<OutputWindowPane>()
						.FirstOrDefault(p => p.Name == "Uno Platform");

					if (owP == null)
					{
						owP = ow
						.OutputWindowPanes
						.Add("Uno Platform");
					}
				}

				return owP;
			};

			_infoAction = s => pane().OutputString("[INFO] " + s + "\r\n");
			_verboseAction = s => pane().OutputString("[VERBOSE] " + s + "\r\n");
			_warningAction = s => pane().OutputString("[WARNING] " + s + "\r\n");
			_errorAction = e => pane().OutputString("[ERROR] " + e.ToString() + "\r\n");
		}
		

		#endregion

		private async Task OnOpened()
		{
			Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "OnOpened: {0}", this.ToString()));

			do
			{
				try
				{
					var componentModel = (IComponentModel)await GetServiceAsync(typeof(SComponentModel));
					IVsPackageInstallerServices installerServices = componentModel.GetService<IVsPackageInstallerServices>();
					var installedPackages = installerServices.GetInstalledPackages();

					var unoNuGetPackage = installedPackages
						.Where(p => p.Id.Equals("Uno.UI.RemoteControl", StringComparison.OrdinalIgnoreCase))
						.OrderByDescending(p => p.VersionString)
						.LastOrDefault();

					if (unoNuGetPackage != null)
					{
						if (string.IsNullOrWhiteSpace(unoNuGetPackage.InstallPath.Trim()))
						{
							_infoAction("The Uno.UI.RemoteControl package has not been restored yet, retrying...");
						}
						else
						{
							var toolsPath = System.IO.Path.Combine(unoNuGetPackage.InstallPath, "tools", "rc");
							var asmPath = System.IO.Path.Combine(toolsPath, "Uno.UI.RemoteControl.VS.dll");
							var asm = System.Reflection.Assembly.LoadFrom(asmPath);

							var entryPointType = asm.GetType("Uno.UI.RemoteControl.VS.EntryPoint");

							if (entryPointType.GetConstructor(
								new[] {
									typeof(DTE2),
									typeof(string),
									typeof(AsyncPackage),
									typeof(Action<Func<Task<Dictionary<string, string>>>>)
								}) != null)
							{
								_remoteControl = Activator.CreateInstance(
									entryPointType,
									_dte,
									toolsPath,
									this,
									(Action<Func<Task<Dictionary<string, string>>>>)SetGlobalVariablesProvider) as IDisposable;

								_infoAction($"Loaded the Uno.UI Remote Control service ({unoNuGetPackage.VersionString}).");
							}
							else
							{
								_infoAction("The loaded solution contains an Uno.UI Remote Control service.");
							}
						}
					}

					if (_dte.Solution.IsOpen)
					{
						return;
					}
				}
				catch (Exception e)
				{
					_errorAction(e);
				}

				await System.Threading.Tasks.Task.Delay(5000);
			}
			while (true);
		}

		private void SetGlobalVariablesProvider(Func<Task<Dictionary<string, string>>> globalFunctionProvider)
		{
			GlobalFunctionProvider = globalFunctionProvider;
		}
	}
}
