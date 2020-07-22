module Uno.UI.Interop {
	export interface IMonoRuntime {
		assembly_load(assemblyName: string): Interop.IMonoAssemblyHandle;
		find_class(moduleHandle: Interop.IMonoAssemblyHandle, namespace: string, typeName: string): Interop.IMonoClassHandle;
		find_method(classHandle: Interop.IMonoClassHandle, methodName: string, _: number): Interop.IMonoMethodHandle;
		call_method(methodHandle: Interop.IMonoMethodHandle, object: any, params?: any[]): any;
		mono_string(str: string): Interop.IMonoStringHandle;
		conv_string(strHandle: Interop.IMonoStringHandle): string;
	}
}