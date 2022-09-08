
#if __ANDROID__

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.ApplicationModel.Activation
{
	public partial interface IBackgroundActivatedEventArgs
	{
		Background.IBackgroundTaskInstance TaskInstance { get; }
	}
}

#endif
