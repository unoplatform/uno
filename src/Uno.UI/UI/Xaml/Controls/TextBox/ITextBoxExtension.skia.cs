using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Uno.UI.Xaml.Controls;

/// <summary>
/// Contains TextBox extensions, that are applicable to all platforms
/// (with or without Overlay input).
/// </summary>
internal interface ITextBoxExtension
{
	void OnInputReturnTypeChanged();
}
