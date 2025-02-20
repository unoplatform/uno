using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Uno;
using Uno.Foundation.Extensibility;
using Uno.Foundation.Logging;
using Uno.Helpers;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Popups.Internal;

namespace Windows.UI.Popups;

/// <summary>
/// Represents a dialog for showing messages to the user.
/// </summary>
public sealed partial class MessageDialog
{
	/// <summary>
	/// Creates a new instance of the MessageDialog class, using the specified message content and no title.
	/// </summary>
	/// <param name="content">The message displayed to the user.</param>
	public MessageDialog(string content)
		: this(content, "")
	{
	}

	/// <summary>
	/// Creates a new instance of the MessageDialog class, using the specified message content and title.
	/// </summary>
	/// <param name="content">The message displayed to the user.</param>
	/// <param name="title">The title you want displayed on the dialog.</param>
	public MessageDialog(string content, string title)
	{
		// They can both be empty.
		Content = content ?? throw new ArgumentNullException(nameof(content));
		Title = title ?? throw new ArgumentNullException(nameof(title));

		var commands = new ObservableCollection<IUICommand>();
		commands.CollectionChanged += (s, e) => ValidateCommands();

		Commands = commands;
	}

	/// <summary>
	/// This is used to associate the MessageDialog with a window in multi-window environment.
	/// </summary>
	internal object AssociatedWindow { get; set; }

	public IAsyncOperation<IUICommand> ShowAsync()
	{
		VisualTreeHelperProxy.CloseAllFlyouts();

		return AsyncOperation.FromTask<IUICommand>(async ct =>
		{
			if (CoreDispatcher.Main.HasThreadAccess)
			{
				return await ShowInnerAsync(ct);
			}
			else
			{
				var show = default(Task<IUICommand>);
				await CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, () => show = ShowInnerAsync(ct));

				return await show;
			}
		});
	}

	/// <summary>
	/// Gets or sets the index of the command you want to use as the cancel command.
	/// This is the command that fires when users press the ESC key.	
	/// Add the commands before you set the index.
	/// </summary>
	public uint CancelCommandIndex { get; set; } = uint.MaxValue;

	/// <summary>
	/// Gets an array of commands that appear in the command bar of the message dialog. These commands makes the dialog actionable.
	/// </summary>
	/// <remarks>
	/// Maximum of 3 commands is allowed.
	/// </remarks>
	public IList<IUICommand> Commands { get; }

	/// <summary>
	/// Gets or sets the message to be displayed to the user.
	/// </summary>
	public string Content { get; set; }

	/// <summary>
	/// Gets or sets the index of the command you want to use as the default.
	/// This is the command that fires by default when users press the ENTER key.
	/// Add the commands before you set the index.
	/// </summary>
	public uint DefaultCommandIndex { get; set; }

	/// <summary>
	/// Gets or sets the options for a MessageDialog.
	/// </summary>
	public MessageDialogOptions Options { get; set; }

	/// <summary>
	/// Gets or sets the title to display on the dialog, if any.
	/// </summary>
	public string Title { get; set; }

	private async Task<IUICommand> ShowInnerAsync(CancellationToken ct)
	{
#if __IOS__ || __MACOS__ || __ANDROID__ || __WASM__
		if (WinRTFeatureConfiguration.MessageDialog.UseNativeDialog)
		{
			return await ShowNativeAsync(ct);
		}
#endif

		if (!ApiExtensibility.CreateInstance<IMessageDialogExtension>(this, out var extension))
		{
			throw new InvalidOperationException("MessageDialog extension is not registered");
		}

		return await extension.ShowAsync(ct);
	}

	private void ValidateCommands()
	{
#if __ANDROID__
		if (WinRTFeatureConfiguration.MessageDialog.UseNativeDialog)
		{
			ValidateCommandsNative();
			return;
		}
#endif

		if (Commands.Count > 3)
		{
			this.Log().LogError(
				"Maximum of 3 commands is allowed. Further commands will not be displayed. " +
				"On WinUI/UWP adding more commands will cause an exception.");
		}
	}
}
