using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Core;

namespace Windows.UI.Popups
{
	public sealed partial class MessageDialog
	{
		/// <summary>
		/// Creates a new instance of the MessageDialog class, using the specified message content and no title.
		/// </summary>
		/// <param name="content"></param>
		public MessageDialog(string content)
			: this(content, "")
		{
		}

		/// <summary>
		/// Creates a new instance of the MessageDialog class, using the specified message content and title.
		/// </summary>
		/// <param name="content"></param>
		/// <param name="title"></param>
		public MessageDialog(string content, string title)
		{
			if (content == null)
			{
				throw new ArgumentNullException(nameof(content));
			}

			if (title == null)
			{
				throw new ArgumentNullException(nameof(title));
			}

			// They can both be empty.
			Content = content;
			Title = title;

			var collection = new ObservableCollection<IUICommand>();
			collection.CollectionChanged += (s, e) => this.ValidateCommands();

			Commands = collection;
		}

#if __ANDROID__ || __IOS__

		public IAsyncOperation<IUICommand> ShowAsync()
		{
			var invokedCommand = new TaskCompletionSource<IUICommand>();

			return new AsyncOperation<IUICommand>(async ct =>
			{
				if (CoreDispatcher.Main.HasThreadAccess)
				{
					try
					{
						ShowInner(ct, invokedCommand);
					}
					catch (Exception e)
					{
						invokedCommand.TrySetException(e);
					}
				}

				else
				{
					Exception ex = null;
					await CoreDispatcher.Main.RunAsync(CoreDispatcherPriority.Normal, CallShow);

					if (ex != null)
					{
						invokedCommand.TrySetException(ex);
					}

					void CallShow()
					{
						try
						{
							ShowInner(ct, invokedCommand);
						}
						catch (Exception e)
						{
							ex = e;
						}
					}
				}
				return await invokedCommand.Task;
			});
		}
#endif

		public uint CancelCommandIndex { get; set; } = uint.MaxValue;
		public IList<IUICommand> Commands { get; }
		public string Content { get; set; }
		public uint DefaultCommandIndex { get; set; } = 0;
		public MessageDialogOptions Options { get; set; }
		public string Title { get; set; }

		partial void ValidateCommands();
	}
}
