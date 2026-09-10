#if __APPLE_UIKIT__ && !__TVOS__
#nullable enable
using Foundation;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.AuthenticationBroker;
using Windows.Security.Authentication.Web;

namespace Uno.UI.RuntimeTests.Tests.Windows_Security_Authentication_Web;

[TestClass]
public class Given_WebAuthenticationBrokerProvider
{
	private const string ASWebAuthenticationSessionErrorDomain = "com.apple.AuthenticationServices.WebAuthenticationSession";
	private const string SFAuthenticationErrorDomain = "com.apple.SafariServices.Authentication";
	private const int CanceledLoginErrorCode = 1;
	private const int PresentationContextNotProvidedErrorCode = 2;

	[TestMethod]
	public void When_ASWebAuthenticationSession_Canceled_By_User()
	{
		var error = new NSError((NSString)ASWebAuthenticationSessionErrorDomain, CanceledLoginErrorCode);

		var result = WebAuthenticationBrokerProvider.CreateResult(null, error);

		Assert.AreEqual(WebAuthenticationStatus.UserCancel, result.ResponseStatus);
		Assert.IsNull(result.ResponseData);
	}

#if __IOS__
	[TestMethod]
	public void When_SFAuthenticationSession_Canceled_By_User()
	{
		var error = new NSError((NSString)SFAuthenticationErrorDomain, CanceledLoginErrorCode);

		var result = WebAuthenticationBrokerProvider.CreateResult(null, error);

		Assert.AreEqual(WebAuthenticationStatus.UserCancel, result.ResponseStatus);
		Assert.IsNull(result.ResponseData);
	}
#endif

	[TestMethod]
	public void When_Session_Fails()
	{
		var error = new NSError((NSString)ASWebAuthenticationSessionErrorDomain, PresentationContextNotProvidedErrorCode);

		var result = WebAuthenticationBrokerProvider.CreateResult(null, error);

		Assert.AreEqual(WebAuthenticationStatus.ErrorHttp, result.ResponseStatus);
		Assert.IsNotNull(result.ResponseData);
	}

	[TestMethod]
	public void When_Session_Succeeds()
	{
		var callbackUrl = new NSUrl("my-app://callback?code=42");

		var result = WebAuthenticationBrokerProvider.CreateResult(callbackUrl, null);

		Assert.AreEqual(WebAuthenticationStatus.Success, result.ResponseStatus);
		Assert.AreEqual("my-app://callback?code=42", result.ResponseData);
	}
}
#endif
