using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Management.Instrumentation;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.Commands;
using Uno.UI.RemoteControl.VS.IdeChannel;
using Task = System.Threading.Tasks.Task;

namespace Uno.UI.RemoteControl.VS;

internal sealed class UnoMenuCommand : IDisposable
{
	private readonly AsyncPackage _package;
	private OleMenuCommandService CommandService { get; set; }
	private IdeChannelClient IdeChannelClient;
	private DynamicItemMenuCommand? _dynamicMenuCommand;
	private OleMenuCommand? _unoMainMenuItem;
	private static readonly Guid UnoStudioPackageCmdSet = new Guid("6c532d75-ee35-4726-a1cd-338c5243e38f");
	private static readonly int UnoMainMenu = 0x4100;
	private static readonly int DynamicMenuCommandId = 0x4103;

	public List<AddMenuItemRequestIdeMessage> CommandList { get; set; } = [];

	private UnoMenuCommand(
		AsyncPackage package
		, IdeChannelClient ideChannelClient
		, OleMenuCommandService commandService
		, AddMenuItemRequestIdeMessage cr)
	{
		_package = package ?? throw new ArgumentNullException(nameof(_package));
		CommandService = commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
		IdeChannelClient = ideChannelClient ?? throw new ArgumentNullException(nameof(ideChannelClient));
		CommandList.Add(cr);

		var dynamicItemRootId = new CommandID(UnoStudioPackageCmdSet, DynamicMenuCommandId);
		if (commandService.FindCommand(dynamicItemRootId) is not DynamicItemMenuCommand)
		{
			_dynamicMenuCommand = new DynamicItemMenuCommand(
				dynamicItemRootId,
				IsValidDynamicItem,
				OnInvokedDynamicItem,
				OnBeforeQueryStatusDynamicItem);
			commandService.AddCommand(_dynamicMenuCommand);
		}

		var unoMainMenuId = new CommandID(UnoStudioPackageCmdSet, UnoMainMenu);
		if (commandService.FindCommand(unoMainMenuId) is not OleMenuCommand)
		{
			_unoMainMenuItem = new OleMenuCommand(null, unoMainMenuId);
			_unoMainMenuItem.BeforeQueryStatus += OnBeforeQueryStatus;
			commandService.AddCommand(_unoMainMenuItem);
		}

		var dynamicMenuCommandIdId = new CommandID(UnoStudioPackageCmdSet, DynamicMenuCommandId);
		if (commandService.FindCommand(dynamicMenuCommandIdId) is DynamicItemMenuCommand dynamicMenuItem)
		{
			dynamicMenuItem.BeforeQueryStatus += OnBeforeQueryStatus;
		}
	}

	public static async Task<UnoMenuCommand> InitializeAsync(
		AsyncPackage package
		, IdeChannelClient ideChannelClient
		, AddMenuItemRequestIdeMessage cr)
	{
		// Switch to the main thread - the call to AddCommand in DynamicMenu's constructor requires the UI thread.
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

		if (await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
		{
			return new UnoMenuCommand(package, ideChannelClient, commandService, cr);
		}

		throw new InvalidOperationException("IMenuCommandService is not availabe");
	}

	private void OnBeforeQueryStatus(object sender, EventArgs e)
	{
		var command = sender as OleMenuCommand;
		if (command != null && command.Visible == false)
		{
			command.Visible = true;
			command.Enabled = true;
		}
	}

	/// <summary>
	/// True if the command ID is a valid dynamic item
	/// [inside the range of itens, from DynamicMenuCommandId to CommandList.Count]
	/// </summary>
	private bool IsValidDynamicItem(int commandId)
		=> (commandId >= (int)DynamicMenuCommandId) &&
			((commandId - (int)DynamicMenuCommandId) < CommandList.Count);

	private void OnInvokedDynamicItem(object sender, EventArgs args)
	{
		if (IdeChannelClient != null &&
				sender is DynamicItemMenuCommand matchedCommand &&
				TryGetCommandRequestIdeMessage(matchedCommand, out var currentMenu) &&
				currentMenu.Command is Command cmd)
		{
			var cmdMessage =
				new CommandRequestIdeMessage(
					System.Diagnostics.Process.GetCurrentProcess().Id,
					cmd.Name,
					cmd.Parameter);

			// Ignoring the async call as the OleMenuCommand.execHandler is private and not async
			_ = IdeChannelClient.SendToDevServerAsync(cmdMessage, _package.DisposalToken);
		}
	}

	private void OnBeforeQueryStatusDynamicItem(object sender, EventArgs args)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		if (!CommandList.Any())
		{
			return;
		}

		DynamicItemMenuCommand matchedCommand = (DynamicItemMenuCommand)sender;
		matchedCommand.Enabled = true;
		matchedCommand.Visible = true;

		if (TryGetCommandRequestIdeMessage(matchedCommand, out var currentMenu))
		{
			matchedCommand.Text = currentMenu.Command.Text;
		}
		// Clear the ID because we are done with this item.
		matchedCommand.MatchedCommandId = 0;
	}

	private static int GetCurrentPosition(DynamicItemMenuCommand matchedCommand)
		// The position of the command is the command ID minus the ID of the root dynamic start item.
		=> matchedCommand.MatchedCommandId == 0 ? 0 : matchedCommand.MatchedCommandId - (int)DynamicMenuCommandId;

	private bool TryGetCommandRequestIdeMessage(DynamicItemMenuCommand matchedCommand, [NotNullWhen(true)] out AddMenuItemRequestIdeMessage result)
		=> (result = CommandList.Skip(GetCurrentPosition(matchedCommand)).FirstOrDefault()) != null;

	public void Dispose()
	{
		if (_dynamicMenuCommand is not null)
		{
			CommandService.RemoveCommand(_dynamicMenuCommand);
		}

		if (_unoMainMenuItem is not null)
		{
			CommandService.RemoveCommand(_unoMainMenuItem);
		}
	}
}
