using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Windows.UI.ViewManagement;

internal interface IInputPaneExtension
{
	bool TryHide();

	bool TryShow();
}
