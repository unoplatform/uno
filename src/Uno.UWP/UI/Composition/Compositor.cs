
using Windows.UI;

namespace Windows.UI.Composition
{
	public partial class Compositor : global::System.IDisposable
	{
		private static object _gate = new object();

		public static Compositor Current { get; } = new Compositor();

		public ContainerVisual CreateContainerVisual()
		{
			return new ContainerVisual()
			{
			};
		}

		public SpriteVisual CreateSpriteVisual()
		{
			return new SpriteVisual()
			{
			};
		}

		public CompositionColorBrush CreateColorBrush()
		{
			return new CompositionColorBrush()
			{
			};
		}

		public CompositionColorBrush CreateColorBrush(Color color)
		{
			return new CompositionColorBrush()
			{
				Color = color
			};
		}

		public ScalarKeyFrameAnimation CreateScalarKeyFrameAnimation()
		{
			return new ScalarKeyFrameAnimation();
		}

		public CompositionScopedBatch CreateScopedBatch(CompositionBatchTypes batchType)
		{
			return new CompositionScopedBatch(batchType);
		}
	}
}
