using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.Commands;
using Uno.UI.RemoteControl.VS.IdeChannel;
using Task = System.Threading.Tasks.Task;

namespace Uno.UI.RemoteControl.VS;

internal sealed class UnoMenuCommand
{
	private int rootItemId;
	private readonly AsyncPackage _package;
	//private IAsyncServiceProvider ServiceProvider { get { return this._package; } }

	private IdeChannelClient IdeChannelClient;

	public static UnoMenuCommand? Instance { get; private set; }

	public List<CommandRequestIdeMessage> CommandList { get; set; } = [];

	public OleMenuCommandService CommandService { get; private set; }

	public static readonly Guid PackageUnoGuidString = new Guid("e2245c5b-bbe5-40c8-96d6-94ea655a5ff7");
	public static readonly Guid UnoStudioPackageCmdSet = new Guid("6c532d75-ee35-4726-a1cd-338c5243e38f");
	public static readonly int UnoMainMenu = 0x4100;
	public static readonly int DynamicMenuCommandId = 0x4103;
	public static readonly int AnchorMenuCommandId = 0x4104;

	private UnoMenuCommand(AsyncPackage package, IdeChannelClient ideChannelClient, OleMenuCommandService commandService)
	{
		_package = package ?? throw new ArgumentNullException(nameof(_package));
		CommandService = commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
		IdeChannelClient = ideChannelClient ?? throw new ArgumentNullException(nameof(ideChannelClient));

		CommandID dynamicItemRootId = new CommandID(UnoStudioPackageCmdSet, DynamicMenuCommandId);
		rootItemId = dynamicItemRootId.ID;
		if (commandService.FindCommand(dynamicItemRootId) is not DynamicItemMenuCommand)
		{
			DynamicItemMenuCommand dynamicMenuCommand = new DynamicItemMenuCommand(
				dynamicItemRootId,
				IsValidDynamicItem,
				OnInvokedDynamicItem,
				OnBeforeQueryStatusDynamicItem);
			commandService.AddCommand(dynamicMenuCommand);
		}
	}

	public static async Task InitializeAsync(AsyncPackage package, IdeChannelClient ideChannelClient, CommandRequestIdeMessage cr)
	{
		// Switch to the main thread - the call to AddCommand in DynamicMenu's constructor requires the UI thread.
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);
		if (Instance is null
			&& await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
		{
			Instance = new UnoMenuCommand(package, ideChannelClient, commandService);
			Instance.CommandList.Add(cr);

			CommandID initialCommandId = new CommandID(UnoStudioPackageCmdSet, AnchorMenuCommandId);
			if (Instance.CommandService.FindCommand(initialCommandId) is not OleMenuCommand)
			{
				var menuItem = new OleMenuCommand(null, initialCommandId);
				menuItem.BeforeQueryStatus += Instance.OnBeforeQueryStatusHide;
				commandService.AddCommand(menuItem);
			}

			CommandID unoMainMenuId = new CommandID(UnoStudioPackageCmdSet, UnoMainMenu);
			if (Instance.CommandService.FindCommand(unoMainMenuId) is not OleMenuCommand)
			{
				var unoMenuItem = new OleMenuCommand(null, unoMainMenuId);
				unoMenuItem.BeforeQueryStatus += Instance.OnBeforeQueryStatus;
				commandService.AddCommand(unoMenuItem);
			}

			CommandID dynamicMenuCommandIdId = new CommandID(UnoStudioPackageCmdSet, DynamicMenuCommandId);
			if (Instance.CommandService.FindCommand(dynamicMenuCommandIdId) is DynamicItemMenuCommand dynamicMenuItem)
			{
				dynamicMenuItem.BeforeQueryStatus += Instance.OnBeforeQueryStatus;
			}
		}
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

	private void OnBeforeQueryStatusHide(object sender, EventArgs e)
	{
		var command = sender as OleMenuCommand;
		if (command != null && command.Visible == true)
		{
			command.Visible = false;
		}
	}

	private bool IsValidDynamicItem(int commandId)
	{
		ThreadHelper.ThrowIfNotOnUIThread();

		// The match is valid if the command ID is >= the id of our root dynamic start item
		// and the command ID minus the ID of our root dynamic start item
		// is less than or equal to the number of projects in the solution.
		return (commandId >= (int)DynamicMenuCommandId) &&
			((commandId - (int)DynamicMenuCommandId - 1) < CommandList.Count);
	}

	private void OnInvokedDynamicItem(object sender, EventArgs args)
	{
		DynamicItemMenuCommand matchedCommand = (DynamicItemMenuCommand)sender;

		// The position of the command is the command ID minus the ID of the root dynamic start item.
		// And -1 because the first command is at position 0.
		int commandPosition = matchedCommand.MatchedCommandId - (int)DynamicMenuCommandId - 1;
		if (IdeChannelClient != null && CommandList.Skip(commandPosition).First() is CommandRequestIdeMessage currentCommand)
		{
			//ignoring the async call as the OleMenuCommand.execHandler is private and not async
			_ = IdeChannelClient.SendToDevServerAsync(currentCommand, _package.DisposalToken);
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

		// The position of the command is the command ID minus the ID of the root dynamic start item.
		// And -1 because the first command is at position 0.
		int commandPosition = matchedCommand.MatchedCommandId - (int)DynamicMenuCommandId - 1;

		var currentCommand = CommandList.Skip(commandPosition).First();
		matchedCommand.Text = currentCommand.Command;

		// Clear the ID because we are done with this item.
		matchedCommand.MatchedCommandId = 0;
	}

}
