class SampleRunner {

    static init() {
        this._getAllTests = this.getMethod("[SamplesApp.Wasm] SamplesApp.App:GetAllTests");
        this._runTest = this.getMethod("[SamplesApp.Wasm] SamplesApp.App:RunTest");
        this._isTestDone = this.getMethod("[SamplesApp.Wasm] SamplesApp.App:IsTestDone");
    }

    static getMethod(methodName) {
        var method = Module.mono_bind_static_method(methodName);

        if (!method) {
            throw new `Method ${methodName} does not exist`;
        }

        return method;
    }

    static isTestDone(test) {
        return this._isTestDone(test);
    }

    static runTest(test) {
        return this._runTest(test);
    } 

    static getAllTests() {
        return this._getAllTests();
    } 
}
