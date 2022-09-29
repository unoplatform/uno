#nullable disable

using System.Threading;
using System.Threading.Tasks;

namespace Windows.UI.Popups.Internal;

internal interface IMessageDialogExtension
{
	Task<IUICommand> ShowAsync(CancellationToken ct);
}
