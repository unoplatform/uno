using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Uno.UI.RemoteControl.Messaging.IdeChannel;
using Uno.UI.RemoteControl.VS.Commands;
using Uno.UI.RemoteControl.VS.IdeChannel;
using Task = System.Threading.Tasks.Task;

namespace Uno.UI.RemoteControl.VS;

internal sealed class UnoMenuCommand
{
	private readonly AsyncPackage _package;
	private OleMenuCommandService CommandService { get; set; }
	private IdeChannelClient IdeChannelClient;

	private static readonly Guid UnoStudioPackageCmdSet = new Guid("6c532d75-ee35-4726-a1cd-338c5243e38f");
	private static readonly int UnoMainMenu = 0x4100;
	private static readonly int DynamicMenuCommandId = 0x4103;

	public List<AddMenuItemRequestIdeMessage> CommandList { get; set; } = [];
	public static UnoMenuCommand? Instance { get; private set; }

	private UnoMenuCommand(AsyncPackage package, IdeChannelClient ideChannelClient, OleMenuCommandService commandService, AddMenuItemRequestIdeMessage cr)
	{
		_package = package ?? throw new ArgumentNullException(nameof(_package));
		CommandService = commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
		IdeChannelClient = ideChannelClient ?? throw new ArgumentNullException(nameof(ideChannelClient));
		CommandList.Add(cr);

		CommandID dynamicItemRootId = new CommandID(UnoStudioPackageCmdSet, DynamicMenuCommandId);
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

	public static async Task InitializeAsync(AsyncPackage package, IdeChannelClient ideChannelClient, AddMenuItemRequestIdeMessage cr)
	{
		// Switch to the main thread - the call to AddCommand in DynamicMenu's constructor requires the UI thread.
		await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

		if (Instance is null
			&& await package.GetServiceAsync(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
		{
			Instance = new UnoMenuCommand(package, ideChannelClient, commandService, cr);

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
					System.Diagnostics.Process.GetCurrentProcess().MainWindowHandle.ToInt64(),
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

	private static int GetCurrentPosition(DynamicItemMenuCommand matchedCommand) =>
			// The position of the command is the command ID minus the ID of the root dynamic start item.
			matchedCommand.MatchedCommandId == 0 ? 0 : matchedCommand.MatchedCommandId - (int)DynamicMenuCommandId;

	private bool TryGetCommandRequestIdeMessage(DynamicItemMenuCommand matchedCommand, [NotNullWhen(true)] out AddMenuItemRequestIdeMessage result)
		=> (result = CommandList.Skip(GetCurrentPosition(matchedCommand)).FirstOrDefault()) != null;
}
