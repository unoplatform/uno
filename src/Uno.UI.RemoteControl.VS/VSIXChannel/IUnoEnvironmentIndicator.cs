using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.IDE;

public interface IUnoEnvironmentIndicator
{
	/*
	* WARNING WARNING WARNING WARNING WARNING WARNING WARNING
	*
	* This interface is shared between Uno's VS extension and the Uno.RC package, make sure to keep in sync.
	* In order to avoid versioning issues, avoid modifications and **DO NOT** remove any member from this interface.
	*
	*/

	ValueTask NotifyAsync(string text, CancellationToken ct);
}
