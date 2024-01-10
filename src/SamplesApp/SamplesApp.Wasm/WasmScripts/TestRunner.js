class SampleRunner {

    static init() {

        if (!this._getAllTests) {
            this._getAllTests = this.getMethod("[SamplesApp.Wasm] SamplesApp.App:GetAllTests");
            this._runTest = this.getMethod("[SamplesApp.Wasm] SamplesApp.App:RunTest");
            this._isTestDone = this.getMethod("[SamplesApp.Wasm] SamplesApp.App:IsTestDone");
            this._getDisplayScreenScaling = this.getMethod("[SamplesApp.Wasm] SamplesApp.App:GetDisplayScreenScaling");
        }
    }

    static getMethod(methodName) {
        var method = Module.mono_bind_static_method(methodName);

        if (!method) {
            throw new `Method ${methodName} does not exist`;
        }

        return method;
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
