#nullable enable

using System;
using Microsoft.Extensions.Logging;

namespace Uno.UI.Adapter.Microsoft.Extensions.Logging
{
	using Uno.Foundation.Logging;

	class MicrosoftLoggerFactory : Foundation.Logging.IExternalLoggerFactory, global::Microsoft.Extensions.Logging.ILoggerFactory
	{
		public IExternalLogger CreateLogger(string categoryName) => new MicrosoftLogger(Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory.CreateLogger(categoryName));

		#region Implement global::Microsoft.Extensions.Logging.ILoggerFactory to allow down cast from apps (e.g. runtime tests)
		/// <inheritdoc />
		void global::Microsoft.Extensions.Logging.ILoggerFactory.AddProvider(ILoggerProvider provider)
			=> Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory.AddProvider(provider);

		/// <inheritdoc />
		void IDisposable.Dispose()
			=> Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory.Dispose();

		/// <inheritdoc />
		ILogger global::Microsoft.Extensions.Logging.ILoggerFactory.CreateLogger(string categoryName)
			=> Uno.Extensions.LogExtensionPoint.AmbientLoggerFactory.CreateLogger(categoryName);
		#endregion
	}
}
