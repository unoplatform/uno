using System;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Uno.UI.RemoteControl.VS.AppLaunch;

/// <summary>
/// Bridges IDE events (Play/Run command and solution build lifecycle) to the VsAppLaunchStateService.
/// </summary>
internal sealed class VsAppLaunchIdeBridge : IDisposable
{
	private readonly AsyncPackage _package;
	private readonly DTE2 _dte;
	private readonly VsAppLaunchStateService<AppLaunchDetails> _stateService;
	private IVsSolutionBuildManager2? _sbm;
	private uint _adviseCookie;
	private CommandEvents? _debugStart;
	private CommandEvents? _runNoDebug;
	private _dispCommandEvents_BeforeExecuteEventHandler? _beforeExecuteHandler;

	private VsAppLaunchIdeBridge(AsyncPackage package, DTE2 dte, VsAppLaunchStateService<AppLaunchDetails> stateService)
	{
		_package = package;
		_dte = dte;
		_stateService = stateService;
	}

	public static async Task<VsAppLaunchIdeBridge> CreateAsync(AsyncPackage package, DTE2 dte, VsAppLaunchStateService<AppLaunchDetails> stateService)
	{
		var bridge = new VsAppLaunchIdeBridge(package, dte, stateService);
		await bridge.InitializeAsync();
		return bridge;
	}

	private async Task InitializeAsync()
	{
		_sbm = await _package.GetServiceAsync<SVsSolutionBuildManager, IVsSolutionBuildManager2>();
		Assumes.Present(_sbm);

		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		var sink = new BuildEventsSink(this);
		_sbm.AdviseUpdateSolutionEvents(sink, out _adviseCookie);

		// VSStd97 command set (Play buttons)
		const string std97 = "{5EFC7975-14BC-11CF-9B2B-00AA00573819}";

		_debugStart = _dte.Events.CommandEvents[std97, (int)VSConstants.VSStd97CmdID.Start];
		_runNoDebug = _dte.Events.CommandEvents[std97, (int)VSConstants.VSStd97CmdID.StartNoDebug];

		_beforeExecuteHandler = OnBeforeExecute;
		_debugStart.BeforeExecute += _beforeExecuteHandler;
		_runNoDebug.BeforeExecute += _beforeExecuteHandler;
	}

	private void OnBeforeExecute(string guid, int id, object customIn, object customOut, ref bool cancelDefault)
	{
		// Determine if this is a debug launch based on command ID
		var isDebug = id == (int)VSConstants.VSStd97CmdID.Start; // Start = debug, StartNoDebug = no debug

		// Fire-and-forget; collect details on UI thread when needed
		_package.JoinableTaskFactory.RunAsync(async () =>
		{
			var details = await CollectStartupInfoAsync();
			// Update the details with debug information
			var detailsWithDebug = details with { IsDebug = isDebug };
			_stateService.Start(detailsWithDebug);
		}).FileAndForget("uno/appLaunch/onBeforeExecute");
	}

	private async Task<AppLaunchDetails> CollectStartupInfoAsync()
	{
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

		string? projectPath = null;
		IVsHierarchy? hierarchy = null;

		try
		{
			if (_sbm is not null)
			{
				_sbm.get_StartupProject(out hierarchy);
			}
		}
		catch
		{
			// ignore
		}

		if (hierarchy is not null
			&& hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out object extObj) == VSConstants.S_OK
			&& extObj is Project project)
		{
			projectPath = project.FullName;
		}

		return new AppLaunchDetails(projectPath);
	}

	private sealed class BuildEventsSink(VsAppLaunchIdeBridge owner) : IVsUpdateSolutionEvents2
	{
		public int UpdateSolution_Begin(ref int pfCancelUpdate)
		{
			ThreadHelper.ThrowIfNotOnUIThread();
			pfCancelUpdate = 0;
			owner._package.JoinableTaskFactory.RunAsync(async () =>
			{
				var details = await owner.CollectStartupInfoAsync();
				owner._stateService.NotifyBuild(details, BuildNotification.Began);
			}).FileAndForget("uno/appLaunch/buildBegin");
			return VSConstants.S_OK;
		}

		public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
		{
			owner._package.JoinableTaskFactory.RunAsync(async () =>
			{
				var details = await owner.CollectStartupInfoAsync();
				owner._stateService.NotifyBuild(details, fSucceeded != 0 ? BuildNotification.CompletedSuccess : BuildNotification.CompletedFailure);
			}).FileAndForget("uno/appLaunch/buildDone");
			return VSConstants.S_OK;
		}

		public int UpdateSolution_Cancel()
		{
			owner._package.JoinableTaskFactory.RunAsync(async () =>
			{
				var details = await owner.CollectStartupInfoAsync();
				owner._stateService.NotifyBuild(details, BuildNotification.Canceled);
			}).FileAndForget("uno/appLaunch/buildCancel");
			return VSConstants.S_OK;
		}

		public int UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;
		public int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel) => VSConstants.S_OK;
		public int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel) => VSConstants.S_OK;
		public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => VSConstants.S_OK;
	}

	public void Dispose()
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		try
		{
			if (_adviseCookie != 0 && _sbm is not null)
			{
				_sbm.UnadviseUpdateSolutionEvents(_adviseCookie);
				_adviseCookie = 0;
			}
		}
		catch { }

		try
		{
			if (_debugStart is not null && _beforeExecuteHandler is not null)
			{
				_debugStart.BeforeExecute -= _beforeExecuteHandler;
			}
			if (_runNoDebug is not null && _beforeExecuteHandler is not null)
			{
				_runNoDebug.BeforeExecute -= _beforeExecuteHandler;
			}
		}
		catch { }
	}
}
