using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Xaml.Controls;

internal interface IAppBarCommand
{
	Visibility Visibility { get; }

	string Label { get; }

	CommandBarLabelPosition LabelPosition { get; }
}
