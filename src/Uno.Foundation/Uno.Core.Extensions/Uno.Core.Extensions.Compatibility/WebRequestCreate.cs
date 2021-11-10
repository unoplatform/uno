// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Net;
using Uno.Extensions;

namespace Uno
{
	public class WebRequestCreate : IWebRequestCreate
	{
		public WebRequest Create(Uri uri)
		{
#pragma warning disable SYSLIB0014 // Type or member is obsolete
			return WebRequest.Create(uri);
#pragma warning restore SYSLIB0014 // Type or member is obsolete
		}
	}

	public class FuncWebRequestCreate : IWebRequestCreate
	{
		private readonly Func<Uri, WebRequest> _provider;

		public FuncWebRequestCreate(Func<Uri, WebRequest> provider)
		{
			_provider = provider;
		}

		public WebRequest Create(Uri uri)
		{
			return _provider(uri);
		}
	}

	public class WebRequestCreateDecorator : IWebRequestCreate
	{
		private readonly IWebRequestCreate _webRequestProvider;
		private readonly Func<WebRequest, WebRequest> _decorator;

		public WebRequestCreateDecorator(IWebRequestCreate webRequestProvider, Func<WebRequest, WebRequest> decorator)
		{
			_webRequestProvider = webRequestProvider;
			_decorator = decorator;
		}

		public WebRequest Create(Uri uri)
		{
			return _decorator(_webRequestProvider.Create(uri));
		}
	}

	public static class WebRequestCreateExtensions
	{
		public static IWebRequestCreate Decorate(this IWebRequestCreate webRequestProvider, Func<WebRequest, WebRequest> requestDecorator)
		{
			return new WebRequestCreateDecorator(webRequestProvider, requestDecorator);
		}

		public static IWebRequestCreate Decorate<TRequest>(this IWebRequestCreate webRequestProvider, Func<TRequest, WebRequest> requestDecorator)
			where TRequest : WebRequest
		{
			return new WebRequestCreateDecorator(webRequestProvider, r => (r as TRequest).SelectOrDefault(requestDecorator, r));
		}

		public static IWebRequestCreate Decorate(this IWebRequestCreate webRequestProvider, Action<WebRequest> requestDecorator)
		{
			return new WebRequestCreateDecorator(webRequestProvider, r =>
			{
				requestDecorator(r);
				return r;
			});
		}

		public static IWebRequestCreate Decorate<TRequest>(this IWebRequestCreate webRequestProvider, Action<TRequest> requestDecorator)
			where TRequest : WebRequest
		{
			return new WebRequestCreateDecorator(webRequestProvider, r =>
			{
				r.Maybe(requestDecorator);
				return r;
			});
		}
	}
}