using System;
using System.Threading.Tasks;
using Uno.UI.RemoteControl.HotReload.Messages;

namespace Uno.UI.RemoteControl.Host
{
	public interface IServerProcessor : IDisposable
	{
		string Scope { get; }

		Task ProcessFrame(Frame frame);
	}

	[System.AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
	public sealed class ServerProcessorAttribute : Attribute
	{
		readonly Type processor;

		// This is a positional argument
		public ServerProcessorAttribute(Type processor) => this.processor = processor;

		public Type ProcessorType
			=> processor;
	}
}
