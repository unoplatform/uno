package Uno.UI;

public interface ISamplesAppMainActivity {
	String RunTest(String metadataName);
	boolean IsTestDone(String testId);
	String GetDisplayScreenScaling(String displayId);
	String GetScreenshot(String displayId);
	void SetFullScreenMode(boolean fullscreen);
}
