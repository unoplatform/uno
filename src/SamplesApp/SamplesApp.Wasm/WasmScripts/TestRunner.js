class SampleRunner {

    static async init() {

        if (!this._getAllTests) {
            const sampleAppExports = await Module.getAssemblyExports("SamplesApp.Wasm");

            this._getAllTests = sampleAppExports.SamplesApp.App.GetAllTests;
            this._runTest = sampleAppExports.SamplesApp.App.RunTest;
            this._isTestDone = sampleAppExports.SamplesApp.App.IsTestDone;
            this._getDisplayScreenScaling = sampleAppExports.SamplesApp.App.GetDisplayScreenScaling;
        }
    }

    static IsTestDone(test) {
        SampleRunner.init();
        return this._isTestDone(test);
    }

    static RunTest(test) {
        SampleRunner.init();
        return this._runTest(test);
    } 

    static GetAllTests() {
        SampleRunner.init();
        return this._getAllTests();
    } 

    static GetDisplayScreenScaling(displayId) {
        SampleRunner.init();
        return this._getDisplayScreenScaling(displayId);
    }

    static RefreshBrowser(unused) {
        window.location.reload();
    } 
}
