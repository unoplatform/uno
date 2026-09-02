using Foundation;
using Uno.UI.Runtime.Skia.AppleUIKit;

namespace SamplesApp.iOS;

// UIKit only instantiates this once Info.plist declares a UIApplicationSceneManifest naming it.
// Adding that manifest switches the app to the scene lifecycle, which is its own change.
[Register("SceneDelegate")]
public class SceneDelegate : UnoUISceneDelegate
{
}
