using System;
using System.Runtime.InteropServices;
using Windows.Graphics.Effects;
using Windows.Graphics.Effects.Interop;

namespace Microsoft.Graphics.Canvas.Effects
{
	[Guid("C80ECFF0-3FD5-4F05-8328-C5D1724B4F0A")]
	public class AlphaMaskEffect : ICanvasEffect
	{
		private string _name = "AlphaMaskEffect";
		private Guid _id = new Guid("C80ECFF0-3FD5-4F05-8328-C5D1724B4F0A");

		public string Name
		{
			get => _name;
			set => _name = value;
		}

		public static bool IsSupported => true;

		public IGraphicsEffectSource AlphaMask { get; set; }

		public IGraphicsEffectSource Source { get; set; }

		public CanvasBufferPrecision? BufferPrecision { get; set; }

		public bool CacheOutput { get; set; }

		public Guid GetEffectId() => _id;

		public void GetNamedPropertyMapping(string name, out uint index, out GraphicsEffectPropertyMapping mapping) => throw new NotSupportedException();

		public object GetProperty(uint index) => throw new NotSupportedException();

		public uint GetPropertyCount() => 0;
		public IGraphicsEffectSource GetSource(uint index) => index == 0 ? Source : AlphaMask;
		public uint GetSourceCount() => 2;

		public void Dispose() { }
	}
}
