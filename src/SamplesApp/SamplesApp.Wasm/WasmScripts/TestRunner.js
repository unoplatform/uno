class SampleRunner {

    static async init() {

        if (!SampleRunner._getAllTests) {
            const sampleAppExports = await Module.getAssemblyExports("SamplesApp.Wasm");

            SampleRunner._getAllTests = sampleAppExports.SamplesApp.App.GetAllTests;
            SampleRunner._runTest = sampleAppExports.SamplesApp.App.RunTest;
            SampleRunner._isTestDone = sampleAppExports.SamplesApp.App.IsTestDone;
            SampleRunner._getDisplayScreenScaling = sampleAppExports.SamplesApp.App.GetDisplayScreenScaling;
        }
    }

    static IsTestDone(test) {
        SampleRunner.init();    
        return SampleRunner._isTestDone(test);
    }

    static RunTest(test) {
        SampleRunner.init();
        return SampleRunner._runTest(test);
    } 

    static GetAllTests() {
        SampleRunner.init();
        return SampleRunner._getAllTests();
    } 

    static GetDisplayScreenScaling(displayId) {
        SampleRunner.init();
        return SampleRunner._getDisplayScreenScaling(displayId);
    }

    static RefreshBrowser(unused) {
        window.location.reload();
    } 
}

globalThis.SampleRunner = SampleRunner;
