using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Uno.UI.Tests.Windows_UI_Xaml_Markup.XamlReaderTests;

public partial class VectorPropertyControl : Control
{
	public Vector2 Vec2 { get; set; }

	public Vector3 Vec3 { get; set; }
}
