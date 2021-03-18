require([`${config.uno_app_base}/Uno.UI`], () => {

	MonoSupport.jsCallDispatcher.registerScope("TSBindingsUnitTests", new TSBindingsTests());

});

class TSBindingsTests {

	public TSBindingsTests() {

	}

	public When_IntPtr(pParams: number, pReturn: number): boolean {

		var params = When_IntPtrParams.unmarshal(pParams);

		var ret = new GenericReturn();
		ret.Value = String(params.Id);

		ret.marshal(pReturn);

		return true;
	}

	public When_IntPtr_Zero(pParams: number, pReturn: number): boolean {

		var params = When_IntPtrParams.unmarshal(pParams);

		var ret = new GenericReturn();
		ret.Value = String(params.Id);

		ret.marshal(pReturn);

		return true;
	}

	public When_SingleString(pParams: number, pReturn: number): boolean {

		var params = When_SingleStringParams.unmarshal(pParams);

		var ret = new GenericReturn();
		ret.Value = params.MyString;

		ret.marshal(pReturn);

		return true;
	}

	public When_SingleUnicodeString(pParams: number, pReturn: number): boolean {

		var params = When_SingleStringParams.unmarshal(pParams);

		var ret = new GenericReturn();
		ret.Value = params.MyString;

		ret.marshal(pReturn);

		return true;
	}

	public When_NullString(pParams: number, pReturn: number): boolean {

		var params = When_SingleStringParams.unmarshal(pParams);

		var ret = new GenericReturn();
		ret.Value = String(params.MyString === null);

		ret.marshal(pReturn);

		return true;
	}

	public When_ArrayOfInt(pParams: number, pReturn: number): boolean {

		var params = When_ArrayOfIntParams.unmarshal(pParams);

		var ret = new GenericReturn();
		ret.Value = params.MyArray.join(";");

		ret.marshal(pReturn);

		return true;
	}

	public When_NullArrayOfInt(pParams: number, pReturn: number): boolean {

		var params = When_ArrayOfIntParams.unmarshal(pParams);

		var ret = new GenericReturn();
		ret.Value = String(params.MyArray === null);

		ret.marshal(pReturn);

		return true;
	}

	public When_ArrayOfStrings(pParams: number, pReturn: number): boolean {

		var params = When_ArrayOfStringsParams.unmarshal(pParams);

		var ret = new GenericReturn();
		ret.Value = params.MyArray.join(";");

		ret.marshal(pReturn);

		return true;
	}

	public When_ArrayOfUnicodeStrings(pParams: number, pReturn: number): boolean {

		var params = When_ArrayOfStringsParams.unmarshal(pParams);

		var ret = new GenericReturn();
		ret.Value = params.MyArray.join(";");

		ret.marshal(pReturn);

		return true;
	}

	public When_NullArrayOfStrings(pParams: number, pReturn: number): boolean {

		var params = When_ArrayOfStringsParams.unmarshal(pParams);

		var ret = new GenericReturn();
		ret.Value = String(params.MyArray === null);

		ret.marshal(pReturn);

		return true;
	}

	public When_ArrayOfNullStrings(pParams: number, pReturn: number): boolean {

		var params = When_ArrayOfStringsParams.unmarshal(pParams);

		var ret = new GenericReturn();
		ret.Value = params.MyArray.map(v => v === null).join(";");

		ret.marshal(pReturn);

		return true;
	}
}
