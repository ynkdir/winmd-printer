using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

// https://github.com/microsoft/win32metadata/blob/main/sources/Win32MetadataInterop/SupportedArchitectureAttribute.cs
[Flags]
enum Architecture {
    None = 0,
    X86 = 1,
    X64 = 2,
    Arm64 = 4,
    All = Architecture.X64 | Architecture.X86 | Architecture.Arm64
}

class TypeProvider : ISignatureTypeProvider<string, object>, ICustomAttributeTypeProvider<string> {
    public string GetArrayType(string elementType, ArrayShape shape) {
        Debug.Assert((from x in shape.LowerBounds where x != 0 select x).Count() == 0);
        Debug.Assert(shape.Sizes.Count() == shape.Rank);
        return elementType + String.Join("", from n in shape.Sizes select $"[{n}]");
    }

    public string GetByReferenceType(string elementType) {
        throw new NotImplementedException();
    }

    public string GetFunctionPointerType(MethodSignature<string> signature) {
        throw new NotImplementedException();
    }

    public string GetGenericInstantiation(string genericType, ImmutableArray<string> typeArguments) {
        throw new NotImplementedException();
    }

    public string GetGenericMethodParameter(object genericContext, int index) {
        throw new NotImplementedException();
    }

    public string GetGenericTypeParameter(object genericContext, int index) {
        throw new NotImplementedException();
    }

    public string GetModifiedType(string modifierType, string unmodifiedType, bool isRequired) {
        throw new NotImplementedException();
    }

    public string GetPinnedType(string elementType) {
        throw new NotImplementedException();
    }

    public string GetPointerType(string elementType) {
        return $"{elementType}*";
    }

    public string GetPrimitiveType(PrimitiveTypeCode typeCode) {
        return typeCode switch {
            PrimitiveTypeCode.Boolean => "Boolean",
            PrimitiveTypeCode.Byte => "Byte",
            PrimitiveTypeCode.Char => "Char",
            PrimitiveTypeCode.Double => "Double",
            PrimitiveTypeCode.Int16 => "Int64",
            PrimitiveTypeCode.Int32 => "Int32",
            PrimitiveTypeCode.Int64 => "Int64",
            PrimitiveTypeCode.IntPtr => "IntPtr",
            PrimitiveTypeCode.Object => "Object",
            PrimitiveTypeCode.SByte => "SByte",
            PrimitiveTypeCode.Single => "Single",
            PrimitiveTypeCode.String => "String",
            PrimitiveTypeCode.TypedReference => "TypedReference",
            PrimitiveTypeCode.UInt16 => "Uint16",
            PrimitiveTypeCode.UInt32 => "Uint32",
            PrimitiveTypeCode.UInt64 => "Uint64",
            PrimitiveTypeCode.UIntPtr => "UIntPtr",
            PrimitiveTypeCode.Void => "Void",
            _ => throw new NotImplementedException(),
        };
    }

    public string GetSZArrayType(string elementType) {
        return elementType + "[]";
    }

    public string GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind = 0) {
        var td = reader.GetTypeDefinition(handle);
        return $"{reader.GetString(td.Namespace)}.{reader.GetString(td.Name)}";
    }

    public string GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind = 0) {
        var tr = reader.GetTypeReference((TypeReferenceHandle)handle);
        return $"{reader.GetString(tr.Namespace)}.{reader.GetString(tr.Name)}";
    }

    public string GetTypeFromSpecification(MetadataReader reader, object genericContext, TypeSpecificationHandle handle, byte rawTypeKind = 0) {
        throw new NotImplementedException();
    }

    // ?
    public string GetSystemType() {
        return "System.Type";
    }

    public string GetTypeFromSerializedName(string name) {
        throw new NotImplementedException();
    }

    public PrimitiveTypeCode GetUnderlyingEnumType(string type) {
        return type switch {
            "System.Runtime.InteropServices.CallingConvention" => PrimitiveTypeCode.Int32,
            "Windows.Win32.Interop.Architecture" => PrimitiveTypeCode.Int32,
            _ => throw new NotImplementedException(),
        };
    }

    // ?
    public bool IsSystemType(string type) {
        return type == "System.Type";
    }
}

class JsTypeDefinition {
    MetadataReader _reader;
    TypeDefinition _td;

    public JsTypeDefinition(MetadataReader reader, TypeDefinition td) {
        _reader = reader;
        _td = td;
    }

    public string Namespace { get => _reader.GetString(_td.Namespace); }

    public string Name { get => _reader.GetString(_td.Name); }

    public string BaseType { get {
        if (_td.BaseType.Kind == HandleKind.TypeReference) {
            var tr = _reader.GetTypeReference((TypeReferenceHandle)_td.BaseType);
            return $"{_reader.GetString(tr.Namespace)}.{_reader.GetString(tr.Name)}";
        } else if (_td.BaseType.Kind == HandleKind.TypeDefinition) {
            Debug.Assert(_td.BaseType.IsNil);
            return "TypeDefinition";
        } else if (_td.BaseType.Kind == HandleKind.TypeSpecification) {
            throw new NotImplementedException();
        } else {
            throw new NotImplementedException();
        }
    } }

    public bool IsNested { get => _td.IsNested; }

    public List<string> Attributes { get => _td.Attributes.ToString().Split(", ").ToList(); }

    public List<JsCustomAttribute> CustomAttributes { get =>
        (from h in _td.GetCustomAttributes()
         select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h))).ToList(); }

    public List<JsFieldDefinition> Fields { get =>
        (from h in _td.GetFields()
         select new JsFieldDefinition(_reader, _reader.GetFieldDefinition(h))).ToList(); }

    public List<JsInterfaceImplementation> InterfaceImplementations { get =>
        (from h in _td.GetInterfaceImplementations()
         select new JsInterfaceImplementation(_reader, _reader.GetInterfaceImplementation(h))).ToList(); }

    public JsTypeLayout Layout { get => new JsTypeLayout(_td.GetLayout()); }

    public List<JsMethodDefinition> MethodDefinitions { get =>
        (from h in _td.GetMethods()
         select new JsMethodDefinition(_reader, _reader.GetMethodDefinition(h))).ToList(); }

    public List<JsTypeDefinition> NestedTypes { get =>
        (from h in _td.GetNestedTypes()
         select new JsTypeDefinition(_reader, _reader.GetTypeDefinition(h))).ToList(); }
}

class JsCustomAttribute {
    MetadataReader _reader;
    CustomAttribute _ca;
    MemberReference _mr;
    TypeReference _tr;
    CustomAttributeValue<string> _cv;

    public JsCustomAttribute(MetadataReader reader, CustomAttribute ca) {
        Debug.Assert(ca.Constructor.Kind == HandleKind.MemberReference);
        _reader = reader;
        _ca = ca;
        _mr = _reader.GetMemberReference((MemberReferenceHandle)_ca.Constructor);
        _tr = _reader.GetTypeReference((TypeReferenceHandle)_mr.Parent);
        _cv = _ca.DecodeValue(new TypeProvider());
    }

    public string Type { get => $"{_reader.GetString(_tr.Namespace)}.{_reader.GetString(_tr.Name)}"; }

    public List<JsCustomAttributeFixedArgument> FixedArguments { get =>
        (from ta in _cv.FixedArguments
         select new JsCustomAttributeFixedArgument(ta)).ToList(); }

    public List<JsCustomAttributeNamedArgument> NamedArguments { get =>
        (from na in _cv.NamedArguments
         select new JsCustomAttributeNamedArgument(na)).ToList(); }

    public static object? ToTypedValue(string type, object? val) {
        if (val == null) {
            return null;
        }
        return type switch {
            "System.Runtime.InteropServices.CallingConvention" => ((CallingConvention)val).ToString(),
            "Windows.Win32.Interop.Architecture" => ((Architecture)val).ToString().Split(", ").ToList(),
            _ => val,
        };
    }
}

class JsCustomAttributeFixedArgument {
    CustomAttributeTypedArgument<string> _ta;

    public JsCustomAttributeFixedArgument(CustomAttributeTypedArgument<string> ta) {
        _ta = ta;
    }

    public string Type { get => _ta.Type; }

    public object? Value { get => JsCustomAttribute.ToTypedValue(_ta.Type, _ta.Value); }
}

class JsCustomAttributeNamedArgument {
    CustomAttributeNamedArgument<string> _na;

    public JsCustomAttributeNamedArgument(CustomAttributeNamedArgument<string> na) {
        _na = na;
    }

    public string Kind { get => _na.Kind.ToString(); }

    public string? Name { get => _na.Name; }

    public string Type { get => _na.Type; }

    public object? Value { get => JsCustomAttribute.ToTypedValue(_na.Type, _na.Value); }
}

class JsFieldDefinition {
    MetadataReader _reader;
    FieldDefinition _fd;

    public JsFieldDefinition(MetadataReader reader, FieldDefinition fd) {
        _reader = reader;
        _fd = fd;
    }

    public string Name { get => _reader.GetString(_fd.Name); }

    public string Type { get => _fd.DecodeSignature(new TypeProvider(), new object()); }

    public List<string> Attributes { get => _fd.Attributes.ToString().Split(", ").ToList(); }

    public List<JsCustomAttribute> CustomAttributes { get =>
        (from h in _fd.GetCustomAttributes()
         select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h))).ToList(); }

    public JsConstant? DefaultValue { get =>
        _fd.GetDefaultValue().IsNil ? null : new JsConstant(_reader, _reader.GetConstant(_fd.GetDefaultValue())); }
}

class JsConstant {
    MetadataReader _reader;
    Constant _ct;

    public JsConstant(MetadataReader reader, Constant ct) {
        _reader = reader;
        _ct = ct;
    }

    public string TypeCode { get => _ct.TypeCode.ToString(); }

    public string Value { get {
        var r = _reader.GetBlobReader(_ct.Value);
        return _ct.TypeCode switch {
            ConstantTypeCode.Boolean        => r.ReadBoolean().ToString(),
            ConstantTypeCode.Byte           => r.ReadByte().ToString(),
            ConstantTypeCode.Char           => r.ReadChar().ToString(),
            ConstantTypeCode.Double         => r.ReadDouble().ToString(),
            ConstantTypeCode.Int16          => r.ReadInt16().ToString(),
            ConstantTypeCode.Int32          => r.ReadInt32().ToString(),
            ConstantTypeCode.Int64          => r.ReadInt64().ToString(),
            ConstantTypeCode.SByte          => r.ReadSByte().ToString(),
            ConstantTypeCode.Single         => r.ReadSingle().ToString(),
            ConstantTypeCode.String         => r.ReadUTF16(r.Length),
            ConstantTypeCode.UInt16         => r.ReadUInt16().ToString(),
            ConstantTypeCode.UInt32         => r.ReadUInt32().ToString(),
            ConstantTypeCode.UInt64         => r.ReadUInt64().ToString(),
            ConstantTypeCode.Invalid        => throw new NotImplementedException(),
            ConstantTypeCode.NullReference  => throw new NotImplementedException(),
            _                               => throw new NotImplementedException(),
        };
    } }
}

class JsInterfaceImplementation {
    MetadataReader _reader;
    InterfaceImplementation _ii;

    public JsInterfaceImplementation(MetadataReader reader, InterfaceImplementation ii) {
        _reader = reader;
        _ii = ii;
    }

    public string Interface { get {
        Debug.Assert(_ii.Interface.Kind == HandleKind.TypeReference);
        var tr = _reader.GetTypeReference((TypeReferenceHandle)_ii.Interface);
        return $"{_reader.GetString(tr.Namespace)}.{_reader.GetString(tr.Name)}";
    } }

    public List<JsCustomAttribute> CustomAttributes { get =>
        (from h in _ii.GetCustomAttributes()
         select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h))).ToList(); }
}

class JsTypeLayout {
    TypeLayout _tl;

    public JsTypeLayout(TypeLayout tl) {
        _tl = tl;
    }

    public bool IsDefault { get => _tl.IsDefault; }

    public int PackingSize { get => _tl.PackingSize; }

    public int Size { get => _tl.Size; }
}

class JsMethodDefinition {
    MetadataReader _reader;
    MethodDefinition _md;

    public JsMethodDefinition(MetadataReader reader, MethodDefinition md) {
        _reader = reader;
        _md = md;
    }

    public string Name { get => _reader.GetString(_md.Name); }

    public List<string> Attributes { get => _md.Attributes.ToString().Split(", ").ToList(); }

    public List<JsCustomAttribute> CustomAttributes { get =>
        (from h in _md.GetCustomAttributes()
         select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h))).ToList(); }

    public List<string> ImplAttributes { get => _md.ImplAttributes.ToString().Split(", ").ToList(); }

    public int RelativeVirtualAddress { get => _md.RelativeVirtualAddress; }

    public JsMethodImport? Import { get =>
        _md.GetImport().Module.IsNil ? null : new JsMethodImport(_reader, _md.GetImport()); }

    public JsReturnType ReturnType { get {
        var sig = _md.DecodeSignature(new TypeProvider(), new object());
        // Return type is pa.SequenceNumber == 0.  It can be missing in GetParameters();
        var ps = from h in _md.GetParameters()
                 let pa = _reader.GetParameter(h)
                 where pa.SequenceNumber == 0
                 select pa;
        return new JsReturnType(_reader, ps.Count() == 0 ? null : ps.First(), sig.ReturnType);
    } }

    public List<JsParameter> Parameters { get {
        var sig = _md.DecodeSignature(new TypeProvider(), new object());
        return (from h in _md.GetParameters()
                let pa = _reader.GetParameter(h)
                where pa.SequenceNumber != 0
                orderby pa.SequenceNumber   // seems not needed
                select new JsParameter(_reader, pa, sig.ParameterTypes[pa.SequenceNumber - 1]))
                .ToList();

    } }
}

class JsReturnType {
    MetadataReader _reader;
    Parameter? _pa;
    string _type;

    public JsReturnType(MetadataReader reader, Parameter? pa, string type) {
        _reader = reader;
        _pa = pa;
        _type = type;
    }

    public string Type { get => _type; }

    public List<string> Attributes { get =>
        _pa.HasValue
            ? _pa.Value.Attributes.ToString().Split(", ").ToList()
            : new List<string>(); }

    public List<JsCustomAttribute> CustomAttributes { get =>
        _pa.HasValue
            ?  (from h in _pa.Value.GetCustomAttributes()
                select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h))).ToList()
            : new List<JsCustomAttribute>(); }
}

class JsParameter {
    MetadataReader _reader;
    Parameter _pa;
    string _type;

    public JsParameter(MetadataReader reader, Parameter pa, string type) {
        _reader = reader;
        _pa = pa;
        _type = type;
    }

    public string Name { get => _reader.GetString(_pa.Name); }

    public string Type { get => _type; }

    public int SequenceNumber { get => _pa.SequenceNumber; }

    public List<string> Attributes { get => _pa.Attributes.ToString().Split(", ").ToList(); }

    public List<JsCustomAttribute> CustomAttributes { get =>
        (from h in _pa.GetCustomAttributes()
         select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h))).ToList(); }

    public JsConstant? DefaultValue { get =>
        _pa.GetDefaultValue().IsNil ? null : new JsConstant(_reader, _reader.GetConstant(_pa.GetDefaultValue())); }

}

class JsMethodImport {
    MetadataReader _reader;
    MethodImport _mi;

    public JsMethodImport(MetadataReader reader, MethodImport mi) {
        _reader = reader;
        _mi = mi;
    }

    public string Name { get => _reader.GetString(_mi.Name); }

    public List<string> Attributes { get => _mi.Attributes.ToString().Split(", ").ToList(); }

    public JsModuleReference Module { get => new JsModuleReference(_reader, _reader.GetModuleReference(_mi.Module)); }
}

class JsModuleReference {
    MetadataReader _reader;
    ModuleReference _mr;

    public JsModuleReference(MetadataReader reader, ModuleReference mr) {
        _reader = reader;
        _mr = mr;
    }

    public string Name { get => _reader.GetString(_mr.Name); }

    public List<JsCustomAttribute> CustomAttributes { get =>
        (from h in _mr.GetCustomAttributes()
         select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h))).ToList(); }
}

class MetadataPrinter {
    public static void Main(string[] args) {
        using var fs = new FileStream(args[0], FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var pe = new PEReader(fs);
        var reader = pe.GetMetadataReader();
        Console.WriteLine(JsonSerializer.Serialize(
            from h in reader.TypeDefinitions
            let td = new JsTypeDefinition(reader, reader.GetTypeDefinition(h))
            //let name = $"{td.Namespace}.{td.Name}"
            //where false
            //|| name == "Windows.Win32.Foundation.Apis"                 // Constant, Function
            //|| name == "Windows.Win32.Foundation.HANDLE"               // NativeTypedef
            //|| name == "Windows.Win32.Foundation.WIN32_ERROR"          // Enum
            //|| name == "Windows.Win32.Foundation.RECT"                 // Struct
            //|| name == "Windows.Win32.Foundation.DECIMAL"              // Struct Nested
            //|| name == "Windows.Win32.UI.WindowsAndMessaging.WNDPROC"  // FunctionPointer
            //|| name == "Windows.Win32.System.Com.IUnknown"             // Com
            //|| name == "Windows.Win32.System.Com.IEnumGUID"            // Com
            //// winmd have multiple entry with same name for different architecture or something.
            //|| name == "Windows.Win32.System.SystemServices.POUT_OF_PROCESS_FUNCTION_TABLE_CALLBACK"
            select td));
    }
}
