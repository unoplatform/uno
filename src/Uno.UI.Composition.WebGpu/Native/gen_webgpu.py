#!/usr/bin/env python3
"""Generate a C# interop layer from wgpu-native's modern webgpu.h (+ wgpu.h).
Emits enums, flag-enums, opaque handles, structs, and [DllImport] functions.
Pointers -> IntPtr (auto-sizes 4 on wasm32 / 8 on x64), size_t -> nuint."""
import re, sys

def strip_comments(s):
    s = re.sub(r'/\*.*?\*/', '', s, flags=re.S)
    s = re.sub(r'//[^\n]*', '', s)
    return s

PRIM = {
    'uint64_t':'ulong','int64_t':'long','uint32_t':'uint','int32_t':'int',
    'uint16_t':'ushort','int16_t':'short','uint8_t':'byte','int8_t':'sbyte',
    'size_t':'nuint','float':'float','double':'double','void':'void',
    'WGPUBool':'uint','WGPUFlags':'ulong','char':'byte','int':'int',
}

def map_type(ctype, enums, flags, handles, structs):
    t = ctype.strip()
    t = t.replace('WGPU_NULLABLE','').strip()
    is_const = 'const' in t
    t = t.replace('const','').strip()
    ptr = t.count('*')
    base = t.replace('*','').strip()
    if ptr > 0:
        # typed pointers for struct/primitive pointees (ergonomic + type-safe); IntPtr for
        # opaque handles, callbacks, void*, and char* (strings are handled via WGPUStringView).
        if base in structs: return base + '*' * ptr
        if base in enums or base in flags: return base + '*' * ptr
        if base in PRIM and base not in ('void','char'): return PRIM[base] + '*' * ptr
        return 'IntPtr'
    if base in PRIM: return PRIM[base]
    if base in enums: return base
    if base in flags: return base
    if base in handles: return 'IntPtr'
    if base in structs: return base
    # unknown -> assume enum/uint fallback handled later; keep name
    return base

def main(headers, out):
    text = ''
    for h in headers:
        text += strip_comments(open(h).read()) + '\n'

    enums = {}      # name -> [(member, value)]
    flags = {}      # name -> [(member, value)]  (ulong)
    handles = set()
    structs = {}    # name -> [(ctype, fieldname)]
    funcs = []      # (ret, name, [(ctype, arg)])

    # primitive typedefs: typedef <prim> WGPUName;  (e.g. WGPUSubmissionIndex = uint64_t)
    for m in re.finditer(r'typedef (uint64_t|int64_t|uint32_t|int32_t|uint16_t|int16_t|uint8_t|int8_t|float|double)\s+(WGPU\w+)\s*;', text):
        if m.group(2) not in PRIM:
            PRIM[m.group(2)] = PRIM[m.group(1)]

    # opaque handles: typedef struct WGPUXxxImpl* WGPUXxx;
    for m in re.finditer(r'typedef struct (\w+Impl)\s*\*\s*(\w+)(?:\s+WGPU_\w+)?\s*;', text):
        handles.add(m.group(2))

    # function-pointer typedefs (callbacks) -> IntPtr, incl. WGPUProc
    for m in re.finditer(r'typedef\s+[\w\s\*]+\(\s*\*\s*(\w+)\s*\)\s*\(', text):
        handles.add(m.group(1))

    # C integer constant expressions: hex/dec literals plus shifts/ors/parens (e.g. `1 << 3`,
    # `(1 << 0) | (1 << 2)`). The value charset is validated before eval so nothing else runs.
    def c_const(expr):
        expr = expr.strip()
        if re.fullmatch(r'0[xX][0-9a-fA-F]+|\d+', expr):
            return expr
        if re.fullmatch(r'[0-9\s()<|]+', expr.replace('<<', '<')):
            return f'0x{eval(expr):08X}'
        return None

    # enums: typedef enum WGPUName { ... } WGPUName ...;
    for m in re.finditer(r'typedef enum (\w+)\s*\{(.*?)\}\s*\1', text, re.S):
        name, body = m.group(1), m.group(2)
        members=[]
        for em in re.finditer(r'(\w+)\s*=\s*([^,}]+)', body):
            if (val := c_const(em.group(2))) is not None:
                members.append((em.group(1), val))
        enums[name]=members

    # flag types: typedef WGPUFlags WGPUName;  + static const WGPUName WGPUName_X = <const-expr>;
    for m in re.finditer(r'typedef WGPUFlags (\w+)\s*;', text):
        flags[m.group(1)] = []
    for fname in list(flags):
        for em in re.finditer(r'static const '+fname+r'\s+(\w+)\s*=\s*([^;]+);', text):
            if (val := c_const(em.group(2))) is not None:
                flags[fname].append((em.group(1), val))

    # structs: typedef struct WGPUName { ... } WGPUName ...;
    for m in re.finditer(r'typedef struct (\w+)\s*\{(.*?)\}\s*\1', text, re.S):
        name, body = m.group(1), m.group(2)
        if name.endswith('Impl'): continue
        if 'union' in body: continue   # anonymous unions can't be expressed; hand-add if needed
        fields=[]
        for line in body.split(';'):
            line=line.strip()
            if not line: continue
            # split type vs name (last token is the field name, possibly with *)
            mm = re.match(r'^(.*?)(\**)\s*(\w+)$', line.replace('\n',' ').strip())
            if not mm: continue
            ctype = (mm.group(1)+mm.group(2)).strip()
            fname = mm.group(3)
            fields.append((ctype, fname))
        structs[name]=fields

    # prune structs that have a by-value field of an undefined type (unused platform/union chains)
    def base_of(ct):
        t=ct.replace('WGPU_NULLABLE','').replace('const','').strip()
        return t.count('*'), t.replace('*','').strip()
    changed=True
    while changed:
        changed=False
        known=set(PRIM)|set(enums)|set(flags)|set(handles)|set(structs)
        for name in list(structs):
            for ct,fn in structs[name]:
                ptr,base=base_of(ct)
                if ptr==0 and base not in known:
                    del structs[name]; changed=True; break

    # functions: WGPU_EXPORT ret wgpuXxx(args) ...;   (standard webgpu.h form)
    def parse_args(args):
        arglist=[]
        if args and args!='void':
            for a in args.split(','):
                a=a.strip()
                mm=re.match(r'^(.*?)(\**)\s*(\w+)$', a)
                if mm:
                    arglist.append(((mm.group(1)+mm.group(2)).strip(), mm.group(3)))
        return arglist
    for m in re.finditer(r'WGPU_EXPORT\s+(.+?)\s+(wgpu\w+)\s*\((.*?)\)\s*WGPU_FUNCTION_ATTRIBUTE', text, re.S):
        funcs.append((m.group(1).strip(), m.group(2), parse_args(m.group(3).strip())))
    # wgpu.h native-only extras: plain `ret wgpuXxx(args);` inside extern "C" (no WGPU_EXPORT).
    seen_fn = {n for _,n,_ in funcs}
    for m in re.finditer(r'(?<=[;{}])\s*([A-Za-z_]\w*\s*\**)\s+(wgpu\w+)\s*\(([^;{}]*?)\)\s*;', text, re.S):
        ret, name, args = m.group(1).strip(), m.group(2), m.group(3).strip()
        if name in seen_fn: continue
        seen_fn.add(name)
        funcs.append((ret, name, parse_args(args)))

    # ---- emit ----
    o=[]
    o.append('// <auto-generated> from wgpu-native webgpu.h (modern ABI). DO NOT EDIT BY HAND.')
    o.append('#nullable disable')
    o.append('using System;')
    o.append('using System.Runtime.InteropServices;')
    o.append('namespace Uno.WebGpu.Native;')
    o.append('')
    for name, members in enums.items():
        o.append(f'public enum {name} : uint {{')
        for mem,val in members:
            short = mem[len(name)+1:] if mem.startswith(name+'_') else mem
            if short and short[0].isdigit(): short='_'+short
            o.append(f'    {short} = {val},')
        o.append('}')
    for name, members in flags.items():
        o.append('[Flags]')
        o.append(f'public enum {name} : ulong {{')
        for mem,val in members:
            short = mem[len(name)+1:] if mem.startswith(name+'_') else mem
            if short and short[0].isdigit(): short='_'+short
            o.append(f'    {short} = {val},')
        o.append('}')
    # PascalCase the camelCase C field names so they line up with the field names the
    # (Silk-originated) backend already writes — PascalCase(webgpu.h camelCase) == Silk's names.
    def pascal(s):
        return s[:1].upper() + s[1:] if s else s
    for name, fields in structs.items():
        o.append('[StructLayout(LayoutKind.Sequential)]')
        o.append(f'public unsafe struct {name} {{')
        for ctype,fname in fields:
            cs = map_type(ctype, enums, flags, handles, structs)
            o.append(f'    public {cs} {pascal(fname)};')
        o.append('}')
    o.append('public static unsafe partial class WGPU {')
    o.append('    const string L = "webgpu";')
    for ret,name,args in funcs:
        cret = map_type(ret, enums, flags, handles, structs)
        params = ', '.join(f'{map_type(t, enums, flags, handles, structs)} {n}' for t,n in args)
        o.append(f'    [DllImport(L, EntryPoint="{name}")] public static extern {cret} {name}({params});')
    o.append('}')
    open(out,'w').write('\n'.join(o))
    print(f'enums={len(enums)} flags={len(flags)} handles={len(handles)} structs={len(structs)} funcs={len(funcs)} -> {out}')

if __name__=='__main__':
    main(sys.argv[1:-1], sys.argv[-1])
