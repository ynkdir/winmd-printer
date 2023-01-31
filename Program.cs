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

// Windows.Foundation.Metadata.MarshalingType
enum MarshalingType {
    Agile = 2,
    InvalidMarshaling = 0,
    None = 1,
    Standard = 3
}

// Windows.Foundation.Metadata.ThreadingModel
enum ThreadingModel {
    Both = 3,
    InvalidThreading = 0,
    MTA = 2,
    STA = 1
}

// Windows.Foundation.Metadata.DeprecationType
enum DeprecationType {
    Deprecate = 0,
    Remove = 1
}

// Windows.Foundation.Metadata.GCPressureAmount
enum GCPressureAmount {
    High = 2,
    Low = 0,
    Medium = 1
}

// Windows.Foundation.Metadata.CompositionType
enum CompositionType {
    Protected = 1,
    Public = 2
}

class TType {
    public string Kind { get; set; } /* for CS8625 */ = "";
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TType? Type { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Size { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<TType>? TypeArguments { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TType? ModifierType { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public TType? UnmodifiedType { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public bool? IsRequired { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Comment { get; set; }
}

class TGenericContext {
    MetadataReader _reader;
    TypeDefinition _td;
    MethodDefinition? _md;

    public TGenericContext(MetadataReader reader, TypeDefinition td, MethodDefinition? md = null) {
        _reader = reader;
        _td = td;
        _md = md;
    }

    public TType GetMethodParameter(int index) {
        Debug.Assert(_md is not null);
        var gm = _reader.GetGenericParameter(_md.Value.GetGenericParameters()[index]);
        return new TType() {
            Kind = "GenericParameter",
            Name = _reader.GetString(gm.Name)
        };
    }

    public TType GetTypeParameter(int index) {
        var gm = _reader.GetGenericParameter(_td.GetGenericParameters()[index]);
        return new TType() {
            Kind = "GenericParameter",
            Name = _reader.GetString(gm.Name)
        };
    }
}

class TypeProvider : ISignatureTypeProvider<TType, TGenericContext>, ICustomAttributeTypeProvider<TType> {
    public TType GetArrayType(TType elementType, ArrayShape shape) {
        Debug.Assert((from x in shape.LowerBounds where x != 0 select x).Count() == 0);
        Debug.Assert(shape.Sizes.Count() == shape.Rank);
        TType type = elementType;
        foreach (var size in shape.Sizes) {
            type = new TType() {
                Kind = "Array",
                Type = type,
                Size = size
            };
        }
        return type;
    }

    public TType GetByReferenceType(TType elementType) {
        return new TType() {
            Kind = "Reference",
            Type = elementType
        };
    }

    public TType GetFunctionPointerType(MethodSignature<TType> signature) {
        throw new NotImplementedException();
    }

    public TType GetGenericInstantiation(TType genericType, ImmutableArray<TType> typeArguments) {
        Debug.Assert(genericType.Kind == "Type");
        return new TType() {
            Kind = "Generic",
            Type = genericType,
            TypeArguments = typeArguments.ToList()
        };
    }

    public TType GetGenericMethodParameter(TGenericContext genericContext, int index) {
        return genericContext.GetMethodParameter(index);
    }

    public TType GetGenericTypeParameter(TGenericContext genericContext, int index) {
        return genericContext.GetTypeParameter(index);
    }

    public TType GetModifiedType(TType modifierType, TType unmodifiedType, bool isRequired) {
        Debug.Assert(modifierType.Kind == "Type");
        return new TType() {
            Kind = "Modified",
            ModifierType = modifierType,
            UnmodifiedType = unmodifiedType,
            IsRequired = isRequired
        };
    }

    public TType GetPinnedType(TType elementType) {
        throw new NotImplementedException();
    }

    public TType GetPointerType(TType elementType) {
        return new TType() {
            Kind = "Pointer",
            Type = elementType
        };
    }

    public TType GetPrimitiveType(PrimitiveTypeCode typeCode) {
        return new TType() {
            Kind = "Primitive",
            Name = typeCode.ToString()
        };
    }

    public TType GetSZArrayType(TType elementType) {
        return new TType() {
            Kind = "SZArray",
            Type = elementType
        };
    }

    public TType GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind) {
        var td = reader.GetTypeDefinition(handle);
        return new TType() {
            Kind = "Type",
            Name = $"{reader.GetString(td.Namespace)}.{reader.GetString(td.Name)}",
            Comment = "TypeDefinition"
        };
    }

    public TType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) {
        var tr = reader.GetTypeReference(handle);
        return new TType() {
            Kind = "Type",
            Name = $"{reader.GetString(tr.Namespace)}.{reader.GetString(tr.Name)}",
            Comment = "TypeReference"
        };
    }

    public TType GetTypeFromSpecification(MetadataReader reader, TGenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind) {
        throw new NotImplementedException();
    }

    // ?
    public TType GetSystemType() {
        return new TType() {
            Kind = "System.Type"
        };
    }

    public TType GetTypeFromSerializedName(string name) {
        return new TType() {
            Kind = "SerializedName",
            Name = name
        };
    }

    public PrimitiveTypeCode GetUnderlyingEnumType(TType type) {
        return type.Name switch {
            "System.Runtime.InteropServices.CallingConvention" => PrimitiveTypeCode.Int32,
            "Windows.Win32.Interop.Architecture" => PrimitiveTypeCode.Int32,
            "System.Type" => PrimitiveTypeCode.String,
            "System.AttributeTargets" => PrimitiveTypeCode.Int32,
            "Windows.Foundation.Metadata.MarshalingType" => PrimitiveTypeCode.Int32,
            "Windows.Foundation.Metadata.ThreadingModel" => PrimitiveTypeCode.Int32,
            "Windows.Foundation.Metadata.DeprecationType" => PrimitiveTypeCode.Int32,
            "Windows.Foundation.Metadata.GCPressureAmount" => PrimitiveTypeCode.Int32,
            "Windows.Foundation.Metadata.CompositionType" => PrimitiveTypeCode.Int32,
            _ => throw new NotImplementedException(),
        };
    }

    public static object? ToCustomValue(TType type, object? val) {
        return val is null ? null : type.Name switch {
            "System.Runtime.InteropServices.CallingConvention" => ((CallingConvention)val).ToString(),
            "Windows.Win32.Interop.Architecture" => ((Architecture)val).ToString().Split(", ").ToList(),
            "System.Type" => val,
            "System.AttributeTargets" => ((System.AttributeTargets)val).ToString().Split(", ").ToList(),
            "Windows.Foundation.Metadata.MarshalingType" => ((MarshalingType)val).ToString(),
            "Windows.Foundation.Metadata.ThreadingModel" => ((ThreadingModel)val).ToString(),
            "Windows.Foundation.Metadata.DeprecationType" => ((DeprecationType)val).ToString(),
            "Windows.Foundation.Metadata.GCPressureAmount" => ((GCPressureAmount)val).ToString(),
            "Windows.Foundation.Metadata.CompositionType" => ((CompositionType)val).ToString(),
            _ => val,
        };
    }

    // ?
    public bool IsSystemType(TType type) {
        return type.Kind == "System.Type";
    }
}

class JsTypeDefinition {
    MetadataReader _reader;
    TypeDefinition _td;
    TGenericContext _gc;

    public JsTypeDefinition(MetadataReader reader, TypeDefinition td) {
        _reader = reader;
        _td = td;
        _gc = new TGenericContext(_reader, _td);
    }

    public string Namespace { get => _reader.GetString(_td.Namespace); }

    public string Name { get => _reader.GetString(_td.Name); }

    public string? BaseType { get {
        if (_td.BaseType.IsNil) {
            return null;
        } else if (_td.BaseType.Kind == HandleKind.TypeReference) {
            var tr = _reader.GetTypeReference((TypeReferenceHandle)_td.BaseType);
            return $"{_reader.GetString(tr.Namespace)}.{_reader.GetString(tr.Name)}";
        } else if (_td.BaseType.Kind == HandleKind.TypeDefinition) {
            throw new NotImplementedException();
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
         select new JsFieldDefinition(_reader, _reader.GetFieldDefinition(h), _gc)).ToList(); }

    public List<JsInterfaceImplementation> InterfaceImplementations { get =>
        (from h in _td.GetInterfaceImplementations()
         select new JsInterfaceImplementation(_reader, _reader.GetInterfaceImplementation(h), _gc)).ToList(); }

    public JsTypeLayout Layout { get => new JsTypeLayout(_td.GetLayout()); }

    public List<JsMethodDefinition> MethodDefinitions { get =>
        (from h in _td.GetMethods()
         select new JsMethodDefinition(_reader, _reader.GetMethodDefinition(h))).ToList(); }

    public List<JsTypeDefinition> NestedTypes { get =>
        (from h in _td.GetNestedTypes()
         select new JsTypeDefinition(_reader, _reader.GetTypeDefinition(h))).ToList(); }

    public List<JsGenericParameter> GenericParameters { get =>
        (from h in _td.GetGenericParameters()
         select new JsGenericParameter(_reader, _reader.GetGenericParameter(h), _gc)).ToList(); }
}

class JsGenericParameter {
    MetadataReader _reader;
    GenericParameter _gp;
    TGenericContext _gc;

    public JsGenericParameter(MetadataReader reader, GenericParameter gp, TGenericContext gc) {
        _reader = reader;
        _gp = gp;
        _gc = gc;
    }

    public List<string> Attributes { get => _gp.Attributes.ToString().Split(", ").ToList(); }

    public int Index { get => _gp.Index; }

    public string Name { get => _reader.GetString(_gp.Name); }

    public List<JsGenericParameterConstraint> Constraints { get =>
        (from h in _gp.GetConstraints()
         select new JsGenericParameterConstraint(_reader, _reader.GetGenericParameterConstraint(h), _gc)).ToList(); }

    public List<JsCustomAttribute> CustomAttributes { get =>
        (from h in _gp.GetCustomAttributes()
         select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h))).ToList(); }
}

class JsGenericParameterConstraint {
    MetadataReader _reader;
    GenericParameterConstraint _gpc;
    TGenericContext _gc;

    public JsGenericParameterConstraint(MetadataReader reader, GenericParameterConstraint gpc, TGenericContext gc) {
        _reader = reader;
        _gpc = gpc;
        _gc = gc;
    }

    public JsEntityHandle Type { get => new JsEntityHandle(_reader, _gpc.Type, _gc); }

    public List<JsCustomAttribute> CustomAttributes { get =>
        (from h in _gpc.GetCustomAttributes()
         select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h))).ToList(); }
}

class JsCustomAttribute {
    MetadataReader _reader;
    CustomAttribute _ca;
    MemberReference _mr;
    TypeReference _tr;
    CustomAttributeValue<TType> _cv;

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
}

class JsCustomAttributeFixedArgument {
    CustomAttributeTypedArgument<TType> _ta;

    public JsCustomAttributeFixedArgument(CustomAttributeTypedArgument<TType> ta) {
        _ta = ta;
    }

    public TType Type { get => _ta.Type; }

    public object? Value { get => TypeProvider.ToCustomValue(_ta.Type, _ta.Value); }
}

class JsCustomAttributeNamedArgument {
    CustomAttributeNamedArgument<TType> _na;

    public JsCustomAttributeNamedArgument(CustomAttributeNamedArgument<TType> na) {
        _na = na;
    }

    public string Kind { get => _na.Kind.ToString(); }

    public string? Name { get => _na.Name; }

    public TType Type { get => _na.Type; }

    public object? Value { get => TypeProvider.ToCustomValue(_na.Type, _na.Value); }
}

class JsFieldDefinition {
    MetadataReader _reader;
    FieldDefinition _fd;
    TGenericContext _gc;

    public JsFieldDefinition(MetadataReader reader, FieldDefinition fd, TGenericContext gc) {
        _reader = reader;
        _fd = fd;
        _gc = gc;
    }

    public string Name { get => _reader.GetString(_fd.Name); }

    public TType Signature { get => _fd.DecodeSignature(new TypeProvider(), _gc); }

    public List<string> Attributes { get => _fd.Attributes.ToString().Split(", ").ToList(); }

    public List<JsCustomAttribute> CustomAttributes { get =>
        (from h in _fd.GetCustomAttributes()
         select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h))).ToList(); }

    public JsConstant? DefaultValue { get =>
        _fd.GetDefaultValue().IsNil ? null : new JsConstant(_reader, _reader.GetConstant(_fd.GetDefaultValue())); }

    public int Offset { get => _fd.GetOffset(); }

    public int RelativeVirtualAddress { get => _fd.GetRelativeVirtualAddress(); }
}

class JsConstant {
    MetadataReader _reader;
    Constant _ct;

    public JsConstant(MetadataReader reader, Constant ct) {
        _reader = reader;
        _ct = ct;
    }

    public string TypeCode { get => _ct.TypeCode.ToString(); }

    public object? Value { get => _reader.GetBlobReader(_ct.Value).ReadConstant(_ct.TypeCode); }
}

class JsInterfaceImplementation {
    MetadataReader _reader;
    InterfaceImplementation _ii;
    TGenericContext _gc;

    public JsInterfaceImplementation(MetadataReader reader, InterfaceImplementation ii, TGenericContext gc) {
        _reader = reader;
        _ii = ii;
        _gc = gc;
    }

    public JsEntityHandle Interface { get => new JsEntityHandle(_reader, _ii.Interface, _gc); }

    public List<JsCustomAttribute> CustomAttributes { get =>
        (from h in _ii.GetCustomAttributes()
         select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h))).ToList(); }
}

class JsEntityHandle {
    MetadataReader _reader;
    EntityHandle _interface;
    TGenericContext _gc;

    public JsEntityHandle(MetadataReader reader, EntityHandle interface_, TGenericContext gc) {
        _reader = reader;
        _interface = interface_;
        _gc = gc;
        if (_interface.Kind == HandleKind.TypeReference) {
            Kind = "TypeReference";
            TypeReference = new JsTypeReference(_reader, _reader.GetTypeReference((TypeReferenceHandle)_interface));
        } else if (_interface.Kind == HandleKind.TypeDefinition) {
            throw new NotImplementedException();
        } else if (_interface.Kind == HandleKind.TypeSpecification) {
            Kind = "TypeSpecification";
            TypeSpecification = new JsTypeSpecification(_reader, _reader.GetTypeSpecification((TypeSpecificationHandle)_interface), _gc);
        } else {
            throw new NotImplementedException();
        }
    }

    public string Kind { get; set; }

    public JsTypeReference? TypeReference { get; set; }

    public JsTypeSpecification? TypeSpecification { get; set; }
}

class JsTypeReference {
    MetadataReader _reader;
    TypeReference _tr;

    public JsTypeReference(MetadataReader reader, TypeReference tr) {
        _reader = reader;
        _tr = tr;
    }

    public string Name { get => _reader.GetString(_tr.Name); }

    public string Namespace { get => _reader.GetString(_tr.Namespace); }
}

class JsTypeSpecification {
    MetadataReader _reader;
    TypeSpecification _ts;
    TGenericContext _gc;

    public JsTypeSpecification(MetadataReader reader, TypeSpecification ts, TGenericContext gc) {
        _reader = reader;
        _ts = ts;
        _gc = gc;
    }

    public TType Signature { get => _ts.DecodeSignature(new TypeProvider(), _gc); }

    public List<JsCustomAttribute> CustomAttributes { get =>
        (from h in _ts.GetCustomAttributes()
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
    TGenericContext _gc;

    public JsMethodDefinition(MetadataReader reader, MethodDefinition md) {
        _reader = reader;
        _md = md;
        _gc = new TGenericContext(_reader, _reader.GetTypeDefinition(_md.GetDeclaringType()), _md);
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

    public JsMethodSignature Signature { get =>
        new JsMethodSignature(_md.DecodeSignature(new TypeProvider(), _gc)); }

    public List<JsParameter> Parameters { get =>
        (from h in _md.GetParameters()
         select new JsParameter(_reader, _reader.GetParameter(h))) .ToList(); }

    public List<JsGenericParameter> GenericParameters { get =>
        (from h in _md.GetGenericParameters()
         select new JsGenericParameter(_reader, _reader.GetGenericParameter(h), _gc)).ToList(); }
}

class JsMethodSignature {
    MethodSignature<TType> _sig;

    public JsMethodSignature(MethodSignature<TType> sig) {
        _sig = sig;
    }

    public int GenericParameterCount { get => _sig.GenericParameterCount; }

    public JsSignatureHeader Header { get => new JsSignatureHeader(_sig.Header); }

    public List<TType> ParameterTypes { get => _sig.ParameterTypes.ToList(); }

    public int RequiredParameterCount { get => _sig.RequiredParameterCount; }

    public TType ReturnType { get => _sig.ReturnType; }
}

class JsSignatureHeader {
    SignatureHeader _sh;

    public JsSignatureHeader(SignatureHeader sh) {
        _sh = sh;
    }

    public List<string> Attributes { get => _sh.Attributes.ToString().Split(", ").ToList(); }

    public string CallingConvention { get => _sh.CallingConvention.ToString(); }

    public bool HasExplicitThis { get => _sh.HasExplicitThis; }

    public bool IsGeneric { get => _sh.IsGeneric; }

    public bool IsInstance { get => _sh.IsInstance; }

    public string Kind { get => _sh.Kind.ToString(); }
}

class JsParameter {
    MetadataReader _reader;
    Parameter _pa;

    public JsParameter(MetadataReader reader, Parameter pa) {
        _reader = reader;
        _pa = pa;
    }

    public string Name { get => _reader.GetString(_pa.Name); }

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
        var target = args.Length >= 2 ? args[1] : "";
        Console.WriteLine(JsonSerializer.Serialize(
            from h in reader.TypeDefinitions
            let td = new JsTypeDefinition(reader, reader.GetTypeDefinition(h))
            where target == "" || target == td.Namespace || target == $"{td.Namespace}.{td.Name}"
            orderby td.Namespace, td.Name
            select td,
            new JsonSerializerOptions { WriteIndented = true}));
    }
}
