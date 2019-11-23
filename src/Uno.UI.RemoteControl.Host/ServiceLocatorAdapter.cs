using System;
using System.Collections.Generic;
using Microsoft.Practices.ServiceLocation;

namespace Uno.UI.RemoteControl.Host
{
	internal class ServiceLocatorAdapter : IServiceLocator
	{
		private readonly IServiceProvider _provider;

		public ServiceLocatorAdapter(IServiceProvider provider)
		{
			_provider = provider;
		}

		public IEnumerable<object> GetAllInstances(Type serviceType) { yield break; }
		public IEnumerable<TService> GetAllInstances<TService>() { yield break; }
		public object GetInstance(Type serviceType) => _provider.GetService(serviceType);
		public object GetInstance(Type serviceType, string key) => throw new NotSupportedException();
		public TService GetInstance<TService>() => (TService)_provider.GetService(typeof(TService));
		public TService GetInstance<TService>(string key) => throw new NotSupportedException();
		public object GetService(Type serviceType) => _provider.GetService(serviceType);
	}
}
