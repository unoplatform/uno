require([`${config.uno_app_base}/Uno.UI`], () => {
    MonoSupport.jsCallDispatcher.registerScope("TSBindingsUnitTests", new TSBindingsTests());
});
class TSBindingsTests {
    TSBindingsTests() {
    }
    When_IntPtr(pParams, pReturn) {
        var params = When_IntPtrParams.unmarshal(pParams);
        var ret = new GenericReturn();
        ret.Value = String(params.Id);
        ret.marshal(pReturn);
        return true;
    }
    When_IntPtr_Zero(pParams, pReturn) {
        var params = When_IntPtrParams.unmarshal(pParams);
        var ret = new GenericReturn();
        ret.Value = String(params.Id);
        ret.marshal(pReturn);
        return true;
    }
    When_SingleString(pParams, pReturn) {
        var params = When_SingleStringParams.unmarshal(pParams);
        var ret = new GenericReturn();
        ret.Value = params.MyString;
        ret.marshal(pReturn);
        return true;
    }
    When_SingleUnicodeString(pParams, pReturn) {
        var params = When_SingleStringParams.unmarshal(pParams);
        var ret = new GenericReturn();
        ret.Value = params.MyString;
        ret.marshal(pReturn);
        return true;
    }
    When_NullString(pParams, pReturn) {
        var params = When_SingleStringParams.unmarshal(pParams);
        var ret = new GenericReturn();
        ret.Value = String(params.MyString === null);
        ret.marshal(pReturn);
        return true;
    }
    When_ArrayOfInt(pParams, pReturn) {
        var params = When_ArrayOfIntParams.unmarshal(pParams);
        var ret = new GenericReturn();
        ret.Value = params.MyArray.join(";");
        ret.marshal(pReturn);
        return true;
    }
    When_NullArrayOfInt(pParams, pReturn) {
        var params = When_ArrayOfIntParams.unmarshal(pParams);
        var ret = new GenericReturn();
        ret.Value = String(params.MyArray === null);
        ret.marshal(pReturn);
        return true;
    }
    When_ArrayOfStrings(pParams, pReturn) {
        var params = When_ArrayOfStringsParams.unmarshal(pParams);
        var ret = new GenericReturn();
        ret.Value = params.MyArray.join(";");
        ret.marshal(pReturn);
        return true;
    }
    When_ArrayOfUnicodeStrings(pParams, pReturn) {
        var params = When_ArrayOfStringsParams.unmarshal(pParams);
        var ret = new GenericReturn();
        ret.Value = params.MyArray.join(";");
        ret.marshal(pReturn);
        return true;
    }
    When_NullArrayOfStrings(pParams, pReturn) {
        var params = When_ArrayOfStringsParams.unmarshal(pParams);
        var ret = new GenericReturn();
        ret.Value = String(params.MyArray === null);
        ret.marshal(pReturn);
        return true;
    }
    When_ArrayOfNullStrings(pParams, pReturn) {
        var params = When_ArrayOfStringsParams.unmarshal(pParams);
        var ret = new GenericReturn();
        ret.Value = params.MyArray.map(v => v === null).join(";");
        ret.marshal(pReturn);
        return true;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class GenericReturn {
    marshal(pData) {
        {
            const stringLength = lengthBytesUTF8(this.Value);
            const pString = Module._malloc(stringLength + 1);
            stringToUTF8(this.Value, pString, stringLength + 1);
            Module.setValue(pData + 0, pString, "*");
        }
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class When_ArrayOfIntParams {
    static unmarshal(pData) {
        const ret = new When_ArrayOfIntParams();
        {
            ret.MyArray_Length = Number(Module.getValue(pData + 0, "i32"));
        }
        {
            const pArray = Module.getValue(pData + 4, "*");
            if (pArray !== 0) {
                ret.MyArray = new Array();
                for (var i = 0; i < ret.MyArray_Length; i++) {
                    const value = Module.getValue(pArray + i * 4, "*");
                    ret.MyArray.push(Number(value));
                }
            }
            else {
                ret.MyArray = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class When_ArrayOfStringParams {
    static unmarshal(pData) {
        let ret = new When_ArrayOfStringParams();
        {
            ret.MyArray_Length = Number(Module.getValue(pData + 0, "i32"));
        }
        {
            var pArray = Module.getValue(pData + 4, "*");
            if (pArray !== 0) {
                ret.MyArray = new Array();
                for (var i = 0; i < ret.MyArray_Length; i++) {
                    ret.MyArray.push(String(MonoRuntime.conv_string(Module.getValue(pArray + i * 4, "*"))));
                }
            }
            else {
                ret.MyArray = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class When_ArrayOfStringsParams {
    static unmarshal(pData) {
        const ret = new When_ArrayOfStringsParams();
        {
            ret.MyArray_Length = Number(Module.getValue(pData + 0, "i32"));
        }
        {
            const pArray = Module.getValue(pData + 4, "*");
            if (pArray !== 0) {
                ret.MyArray = new Array();
                for (var i = 0; i < ret.MyArray_Length; i++) {
                    const value = Module.getValue(pArray + i * 4, "*");
                    if (value !== 0) {
                        ret.MyArray.push(String(MonoRuntime.conv_string(value)));
                    }
                    else {
                        ret.MyArray.push(null);
                    }
                }
            }
            else {
                ret.MyArray = null;
            }
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class When_IntPtrParams {
    static unmarshal(pData) {
        const ret = new When_IntPtrParams();
        {
            ret.Id = Number(Module.getValue(pData + 0, "*"));
        }
        return ret;
    }
}
/* TSBindingsGenerator Generated code -- this code is regenerated on each build */
class When_SingleStringParams {
    static unmarshal(pData) {
        const ret = new When_SingleStringParams();
        {
            const ptr = Module.getValue(pData + 0, "*");
            if (ptr !== 0) {
                ret.MyString = String(Module.UTF8ToString(ptr));
            }
            else {
                ret.MyString = null;
            }
        }
        return ret;
    }
}
