using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Uno.Diagnostics.Eventing;

namespace Microsoft.UI.Xaml.Controls;

partial class Image
{
	private readonly static IEventProvider _imageTrace = Tracing.Get(TraceProvider.Id);

	public static new class TraceProvider
	{
		public readonly static Guid Id = Guid.Parse("{15E13473-560E-4601-86FF-C9E1EDB73701}");

		public const int Image_SetSourceStart = 1;
		public const int Image_SetSourceStop = 2;
		public const int Image_SetUriStart = 3;
		public const int Image_SetUriStop = 4;
		public const int Image_SetImageStart = 5;
		public const int Image_SetImageStop = 6;
	}
}
