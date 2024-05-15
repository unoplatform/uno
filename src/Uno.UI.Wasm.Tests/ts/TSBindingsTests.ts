require([`${config.uno_app_base}/Uno.UI`], () => {

	MonoSupport.jsCallDispatcher.registerScope("TSBindingsUnitTests", new TSBindingsTests());

	(<any>globalThis).When_SingleStringNet7 = TSBindingsTests.When_SingleStringNet7
});

class TSBindingsTests {
	public TSBindingsTests() {
		// https://github.com/dotnet/runtime/blob/a919d611e832bfee46fc34762f5ded2006c9f16d/src/mono/wasm/runtime/invoke-js.ts
	}

	public static When_SingleStringNet7(value: string): string {
		return value;
	}

	public When_IntPtr(pParams: number, pReturn: number): boolean {

		var params = SamplesApp.UnitTests.TSBindings.When_IntPtrParams.unmarshal(pParams);

		var ret = new SamplesApp.UnitTests.TSBindings.GenericReturn();
		ret.Value = String(params.Id);

		ret.marshal(pReturn);

		return true;
	}

	public When_IntPtr_Zero(pParams: number, pReturn: number): boolean {

		var params = SamplesApp.UnitTests.TSBindings.When_IntPtrParams.unmarshal(pParams);

		var ret = new SamplesApp.UnitTests.TSBindings.GenericReturn();
		ret.Value = String(params.Id);

		ret.marshal(pReturn);

		return true;
	}

	public When_SingleString(pParams: number, pReturn: number): boolean {

		var params = SamplesApp.UnitTests.TSBindings.When_SingleStringParams.unmarshal(pParams);

		var ret = new SamplesApp.UnitTests.TSBindings.GenericReturn();
		ret.Value = params.MyString;

		ret.marshal(pReturn);

		return true;
	}

	public When_SingleUnicodeString(pParams: number, pReturn: number): boolean {

		var params = SamplesApp.UnitTests.TSBindings.When_SingleStringParams.unmarshal(pParams);

		var ret = new SamplesApp.UnitTests.TSBindings.GenericReturn();
		ret.Value = params.MyString;

		ret.marshal(pReturn);

		return true;
	}

	public When_NullString(pParams: number, pReturn: number): boolean {

		var params = SamplesApp.UnitTests.TSBindings.When_SingleStringParams.unmarshal(pParams);

		var ret = new SamplesApp.UnitTests.TSBindings.GenericReturn();
		ret.Value = String(params.MyString === null);

		ret.marshal(pReturn);

		return true;
	}

	public When_ArrayOfInt(pParams: number, pReturn: number): boolean {

		var params = SamplesApp.UnitTests.TSBindings.When_ArrayOfIntParams.unmarshal(pParams);

		var ret = new SamplesApp.UnitTests.TSBindings.GenericReturn();
		ret.Value = params.MyArray.join(";");

		ret.marshal(pReturn);

		return true;
	}

	public When_NullArrayOfInt(pParams: number, pReturn: number): boolean {

		var params = SamplesApp.UnitTests.TSBindings.When_ArrayOfIntParams.unmarshal(pParams);

		var ret = new SamplesApp.UnitTests.TSBindings.GenericReturn();
		ret.Value = String(params.MyArray === null);

		ret.marshal(pReturn);

		return true;
	}

	public When_ArrayOfStrings(pParams: number, pReturn: number): boolean {

		var params = SamplesApp.UnitTests.TSBindings.When_ArrayOfStringsParams.unmarshal(pParams);

		var ret = new SamplesApp.UnitTests.TSBindings.GenericReturn();
		ret.Value = params.MyArray.join(";");

		ret.marshal(pReturn);

		return true;
	}

	public When_ArrayOfUnicodeStrings(pParams: number, pReturn: number): boolean {

		var params = SamplesApp.UnitTests.TSBindings.When_ArrayOfStringsParams.unmarshal(pParams);

		var ret = new SamplesApp.UnitTests.TSBindings.GenericReturn();
		ret.Value = params.MyArray.join(";");

		ret.marshal(pReturn);

		return true;
	}

	public When_NullArrayOfStrings(pParams: number, pReturn: number): boolean {

		var params = SamplesApp.UnitTests.TSBindings.When_ArrayOfStringsParams.unmarshal(pParams);

		var ret = new SamplesApp.UnitTests.TSBindings.GenericReturn();
		ret.Value = String(params.MyArray === null);

		ret.marshal(pReturn);

		return true;
	}

	public When_ArrayOfNullStrings(pParams: number, pReturn: number): boolean {

		var params = SamplesApp.UnitTests.TSBindings.When_ArrayOfStringsParams.unmarshal(pParams);

		var ret = new SamplesApp.UnitTests.TSBindings.GenericReturn();
		ret.Value = params.MyArray.map(v => v === null).join(";");

		ret.marshal(pReturn);

		return true;
	}
}
