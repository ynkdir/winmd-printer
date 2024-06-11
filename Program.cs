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
    public string? Namespace { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Name { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[]? LowerBounds { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? Rank { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int[]? Sizes { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IEnumerable<TType>? TypeArguments { get; set; }
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
        return new TType() {
            Kind = "Array",
            Type = elementType,
            LowerBounds = shape.LowerBounds.ToArray(),
            Rank = shape.Rank,
            Sizes = shape.Sizes.ToArray(),
        };
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
            TypeArguments = typeArguments
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
            Namespace = reader.GetString(td.Namespace),
            Name = reader.GetString(td.Name),
            Comment = "TypeDefinition"
        };
    }

    public TType GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind) {
        var tr = reader.GetTypeReference(handle);
        return new TType() {
            Kind = "Type",
            Namespace = reader.GetString(tr.Namespace),
            Name = reader.GetString(tr.Name),
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
        var name = type.Namespace is null ? type.Name : $"{type.Namespace}.{type.Name}";
        return name switch {
            "System.AttributeTargets" => PrimitiveTypeCode.Int32,
            "System.ComponentModel.EditorBrowsableState" => PrimitiveTypeCode.Int32,
            "System.Diagnostics.DebuggerBrowsableState" => PrimitiveTypeCode.Int32,
            "System.Runtime.InteropServices.CallingConvention" => PrimitiveTypeCode.Int32,
            "System.Type" => PrimitiveTypeCode.String,
            "Windows.Foundation.Metadata.AttributeTargets" => PrimitiveTypeCode.Int32,
            "Windows.Foundation.Metadata.CompositionType" => PrimitiveTypeCode.Int32,
            "Windows.Foundation.Metadata.DeprecationType" => PrimitiveTypeCode.Int32,
            "Windows.Foundation.Metadata.GCPressureAmount" => PrimitiveTypeCode.Int32,
            "Windows.Foundation.Metadata.MarshalingType" => PrimitiveTypeCode.Int32,
            "Windows.Foundation.Metadata.ThreadingModel" => PrimitiveTypeCode.Int32,
            "Windows.Win32.Foundation.Metadata.Architecture" => PrimitiveTypeCode.Int32,
            "Windows.Win32.Interop.Architecture" => PrimitiveTypeCode.Int32,
            _ => throw new NotImplementedException(name),
        };
    }

    public static object? ToCustomValue(TType type, object? val) {
        if (val is null) {
            return null;
        }
        var name = type.Namespace is null ? type.Name : $"{type.Namespace}.{type.Name}";
        return name switch {
            "System.AttributeTargets" => ((System.AttributeTargets)val).ToString().Split(", "),
            "System.Runtime.InteropServices.CallingConvention" => ((CallingConvention)val).ToString(),
            "System.Type" => val,
            "Windows.Foundation.Metadata.AttributeTargets" => ((AttributeTargets)val).ToString(),
            "Windows.Foundation.Metadata.CompositionType" => ((CompositionType)val).ToString(),
            "Windows.Foundation.Metadata.DeprecationType" => ((DeprecationType)val).ToString(),
            "Windows.Foundation.Metadata.GCPressureAmount" => ((GCPressureAmount)val).ToString(),
            "Windows.Foundation.Metadata.MarshalingType" => ((MarshalingType)val).ToString(),
            "Windows.Foundation.Metadata.ThreadingModel" => ((ThreadingModel)val).ToString(),
            "Windows.Win32.Foundation.Metadata.Architecture" => ((Architecture)val).ToString().Split(", "),
            "Windows.Win32.Interop.Architecture" => ((Architecture)val).ToString().Split(", "),
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
            var td = _reader.GetTypeDefinition((TypeDefinitionHandle)_td.BaseType);
            return $"{_reader.GetString(td.Namespace)}.{_reader.GetString(td.Name)}";
        } else if (_td.BaseType.Kind == HandleKind.TypeSpecification) {
            // FIXME: BaseType should not be string?
            var ts = _reader.GetTypeSpecification((TypeSpecificationHandle)_td.BaseType);
            var type = ts.DecodeSignature(new TypeProvider(), _gc);
            return FormatType(type);
        } else {
            throw new ArgumentException();
        }
    } }

    private string FormatType(TType type) {
        if (type.Kind == "Generic") {
            var arguments = FormatTypeArguments(type);
            return $"{type.Type.Namespace}.{type.Type.Name}[{arguments}]";
        } else if (type.Kind == "Type") {
            return $"{type.Namespace}.{type.Name}";
        } else if (type.Kind == "GenericParameter") {
            return type.Name!;
        } else if (type.Kind == "Primitive") {
            return type.Name!;
        } else {
            throw new NotImplementedException(type.Kind);
        }
    }

    private string FormatTypeArguments(TType type) {
        return string.Join(", ", from t in type.TypeArguments select FormatType(t));
    }

    public bool IsNested { get => _td.IsNested; }

    public IEnumerable<string> Attributes { get {
        if ((_td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NotPublic)
            yield return "NotPublic";
        if ((_td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.Public)
            yield return "Public";
        if ((_td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPublic)
            yield return "NestedPublic";
        if ((_td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedPrivate)
            yield return "NestedPrivate";
        if ((_td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamily)
            yield return "NestedFamily";
        if ((_td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedAssembly)
            yield return "NestedAssembly";
        if ((_td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamANDAssem)
            yield return "NestedFamANDAssem";
        if ((_td.Attributes & TypeAttributes.VisibilityMask) == TypeAttributes.NestedFamORAssem)
            yield return "NestedFamORAssem";
        if ((_td.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.AutoLayout)
            yield return "AutoLayout";
        if ((_td.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.SequentialLayout)
            yield return "SequentialLayout";
        if ((_td.Attributes & TypeAttributes.LayoutMask) == TypeAttributes.ExplicitLayout)
            yield return "ExplicitLayout";
        if ((_td.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Class)
            yield return "Class";
        if ((_td.Attributes & TypeAttributes.ClassSemanticsMask) == TypeAttributes.Interface)
            yield return "Interface";
        if (_td.Attributes.HasFlag(TypeAttributes.Abstract))
            yield return "Abstract";
        if (_td.Attributes.HasFlag(TypeAttributes.Sealed))
            yield return "Sealed";
        if (_td.Attributes.HasFlag(TypeAttributes.SpecialName))
            yield return "SpecialName";
        if (_td.Attributes.HasFlag(TypeAttributes.Import))
            yield return "Import";
        // obsolate
        //if (_td.Attributes.HasFlag(TypeAttributes.Serializable))
        //    yield return "Serializable";
        if (_td.Attributes.HasFlag(TypeAttributes.WindowsRuntime))
            yield return "WindowsRuntime";
        if ((_td.Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AnsiClass)
            yield return "AnsiClass";
        if ((_td.Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.UnicodeClass)
            yield return "UnicodeClass";
        if ((_td.Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.AutoClass)
            yield return "AutoClass";
        if ((_td.Attributes & TypeAttributes.StringFormatMask) == TypeAttributes.CustomFormatClass)
            yield return "CustomFormatClass";
        if (_td.Attributes.HasFlag(TypeAttributes.BeforeFieldInit))
            yield return "BeforeFieldInit";
        if (_td.Attributes.HasFlag(TypeAttributes.RTSpecialName))
            yield return "RTSpecialName";
        if (_td.Attributes.HasFlag(TypeAttributes.HasSecurity))
            yield return "HasSecurity";
    } }

    public IEnumerable<JsCustomAttribute> CustomAttributes { get =>
        from h in _td.GetCustomAttributes()
        select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h)); }

    public IEnumerable<JsFieldDefinition> Fields { get =>
        from h in _td.GetFields()
        select new JsFieldDefinition(_reader, _reader.GetFieldDefinition(h), _gc); }

    public IEnumerable<JsInterfaceImplementation> InterfaceImplementations { get =>
        from h in _td.GetInterfaceImplementations()
        select new JsInterfaceImplementation(_reader, _reader.GetInterfaceImplementation(h), _gc); }

    public JsTypeLayout Layout { get => new JsTypeLayout(_td.GetLayout()); }

    public IEnumerable<JsMethodDefinition> Methods { get =>
        from h in _td.GetMethods()
        select new JsMethodDefinition(_reader, _reader.GetMethodDefinition(h)); }

    public IEnumerable<JsTypeDefinition> NestedTypes { get =>
        from h in _td.GetNestedTypes()
        select new JsTypeDefinition(_reader, _reader.GetTypeDefinition(h)); }

    public IEnumerable<JsGenericParameter> GenericParameters { get =>
        from h in _td.GetGenericParameters()
        select new JsGenericParameter(_reader, _reader.GetGenericParameter(h), _gc); }
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

    public IEnumerable<string> Attributes { get => _gp.Attributes.ToString().Split(", "); }

    public int Index { get => _gp.Index; }

    public string Name { get => _reader.GetString(_gp.Name); }

    public IEnumerable<JsGenericParameterConstraint> Constraints { get =>
        from h in _gp.GetConstraints()
        select new JsGenericParameterConstraint(_reader, _reader.GetGenericParameterConstraint(h), _gc); }

    public IEnumerable<JsCustomAttribute> CustomAttributes { get =>
        from h in _gp.GetCustomAttributes()
        select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h)); }
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

    public IEnumerable<JsCustomAttribute> CustomAttributes { get =>
        from h in _gpc.GetCustomAttributes()
        select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h)); }
}

class JsCustomAttribute {
    MetadataReader _reader;
    CustomAttribute _ca;
    CustomAttributeValue<TType> _cv;

    public JsCustomAttribute(MetadataReader reader, CustomAttribute ca) {
        _reader = reader;
        _ca = ca;
        if (ca.Constructor.Kind == HandleKind.MemberReference) {
            var _mr = _reader.GetMemberReference((MemberReferenceHandle)_ca.Constructor);
            var _tr = _reader.GetTypeReference((TypeReferenceHandle)_mr.Parent);
            var Namespace = _reader.GetString(_tr.Namespace);
            var Name = _reader.GetString(_tr.Name);
            Type = $"{Namespace}.{Name}";
        } else if (ca.Constructor.Kind == HandleKind.MethodDefinition) {
            var _md = _reader.GetMethodDefinition((MethodDefinitionHandle)_ca.Constructor);
            var _td = _reader.GetTypeDefinition(_md.GetDeclaringType());
            var Namespace = _reader.GetString(_td.Namespace);
            var Name = _reader.GetString(_td.Name);
            Type = $"{Namespace}.{Name}";
        } else {
            throw new ArgumentException();
        }
        _cv = _ca.DecodeValue(new TypeProvider());
    }

    public string Type { get; set; }

    public IEnumerable<JsCustomAttributeFixedArgument> FixedArguments { get =>
        from ta in _cv.FixedArguments
        select new JsCustomAttributeFixedArgument(ta); }

    public IEnumerable<JsCustomAttributeNamedArgument> NamedArguments { get =>
        from na in _cv.NamedArguments
        select new JsCustomAttributeNamedArgument(na); }
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

    public IEnumerable<string> Attributes { get => _fd.Attributes.ToString().Split(", "); }

    public IEnumerable<JsCustomAttribute> CustomAttributes { get =>
        from h in _fd.GetCustomAttributes()
        select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h)); }

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

    public IEnumerable<JsCustomAttribute> CustomAttributes { get =>
        from h in _ii.GetCustomAttributes()
        select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h)); }
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
            // FIXME: ?
            Kind = "TypeReference";
            TypeReference = new JsTypeReference(_reader, _reader.GetTypeDefinition((TypeDefinitionHandle)_interface));
        } else if (_interface.Kind == HandleKind.TypeSpecification) {
            Kind = "TypeSpecification";
            TypeSpecification = new JsTypeSpecification(_reader, _reader.GetTypeSpecification((TypeSpecificationHandle)_interface), _gc);
        } else {
            throw new ArgumentException();
        }
    }

    public string Kind { get; set; }

    public JsTypeReference? TypeReference { get; set; }

    public JsTypeSpecification? TypeSpecification { get; set; }
}

class JsTypeReference {
    public JsTypeReference(MetadataReader reader, TypeReference tr) {
        Name = reader.GetString(tr.Namespace);
        Namespace = reader.GetString(tr.Name);
        Comment = "TypeReference";
    }

    public JsTypeReference(MetadataReader reader, TypeDefinition td) {
        Name = reader.GetString(td.Namespace);
        Namespace = reader.GetString(td.Name);
        Comment = "TypeDefinition";
    }

    public string Name { get; set; }

    public string Namespace { get; set; }

    public string Comment { get; set; }
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

    public IEnumerable<JsCustomAttribute> CustomAttributes { get =>
        from h in _ts.GetCustomAttributes()
        select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h)); }
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

    public IEnumerable<string> Attributes { get {
        if ((_md.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.PrivateScope)
            yield return "PrivateScope";
        if ((_md.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Private)
            yield return "Private";
        if ((_md.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamANDAssem)
            yield return "FamANDAssem";
        if ((_md.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Assembly)
            yield return "Assembly";
        if ((_md.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Family)
            yield return "Family";
        if ((_md.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.FamORAssem)
            yield return "FamORAssem";
        if ((_md.Attributes & MethodAttributes.MemberAccessMask) == MethodAttributes.Public)
            yield return "Public";
        if (_md.Attributes.HasFlag(MethodAttributes.Static))
            yield return "Static";
        if (_md.Attributes.HasFlag(MethodAttributes.Final))
            yield return "Final";
        if (_md.Attributes.HasFlag(MethodAttributes.Virtual))
            yield return "Virtual";
        if (_md.Attributes.HasFlag(MethodAttributes.HideBySig))
            yield return "HideBySig";
        if (_md.Attributes.HasFlag(MethodAttributes.CheckAccessOnOverride))
            yield return "CheckAccessOnOverride";
        if ((_md.Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.ReuseSlot)
            yield return "ReuseSlot";
        if ((_md.Attributes & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot)
            yield return "NewSlot";
        if (_md.Attributes.HasFlag(MethodAttributes.Abstract))
            yield return "Abstract";
        if (_md.Attributes.HasFlag(MethodAttributes.SpecialName))
            yield return "SpecialName";
        if (_md.Attributes.HasFlag(MethodAttributes.PinvokeImpl))
            yield return "PinvokeImpl";
        if (_md.Attributes.HasFlag(MethodAttributes.UnmanagedExport))
            yield return "UnmanagedExport";
        if (_md.Attributes.HasFlag(MethodAttributes.RTSpecialName))
            yield return "RTSpecialName";
        if (_md.Attributes.HasFlag(MethodAttributes.HasSecurity))
            yield return "HasSecurity";
        if (_md.Attributes.HasFlag(MethodAttributes.RequireSecObject))
            yield return "RequireSecObject";
    } }

    public IEnumerable<JsCustomAttribute> CustomAttributes { get =>
        from h in _md.GetCustomAttributes()
        select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h)); }

    public IEnumerable<string> ImplAttributes { get {
        if ((_md.ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.IL)
            yield return "IL";
        if ((_md.ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Native)
            yield return "Native";
        if ((_md.ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.OPTIL)
            yield return "OPTIL";
        if ((_md.ImplAttributes & MethodImplAttributes.CodeTypeMask) == MethodImplAttributes.Runtime)
            yield return "Runtime";
        if ((_md.ImplAttributes & MethodImplAttributes.ManagedMask) == MethodImplAttributes.Unmanaged)
            yield return "Unmanaged";
        if ((_md.ImplAttributes & MethodImplAttributes.ManagedMask) == MethodImplAttributes.Managed)
            yield return "Managed";
        if (_md.ImplAttributes.HasFlag(MethodImplAttributes.ForwardRef))
            yield return "ForwardRef";
        if (_md.ImplAttributes.HasFlag(MethodImplAttributes.PreserveSig))
            yield return "PreserveSig";
        if (_md.ImplAttributes.HasFlag(MethodImplAttributes.InternalCall))
            yield return "InternalCall";
        if (_md.ImplAttributes.HasFlag(MethodImplAttributes.Synchronized))
            yield return "Synchronized";
        if (_md.ImplAttributes.HasFlag(MethodImplAttributes.NoInlining))
            yield return "NoInlining";
        if (_md.ImplAttributes.HasFlag(MethodImplAttributes.AggressiveInlining))
            yield return "AggressiveInlining";
        if (_md.ImplAttributes.HasFlag(MethodImplAttributes.NoOptimization))
            yield return "NoOptimization";
        if (_md.ImplAttributes.HasFlag(MethodImplAttributes.AggressiveOptimization))
            yield return "AggressiveOptimization";
    } }

    public int RelativeVirtualAddress { get => _md.RelativeVirtualAddress; }

    public JsMethodImport? Import { get =>
        _md.GetImport().Module.IsNil ? null : new JsMethodImport(_reader, _md.GetImport()); }

    public JsMethodSignature Signature { get =>
        new JsMethodSignature(_md.DecodeSignature(new TypeProvider(), _gc)); }

    public IEnumerable<JsParameter> Parameters { get =>
        from h in _md.GetParameters()
        select new JsParameter(_reader, _reader.GetParameter(h)); }

    public IEnumerable<JsGenericParameter> GenericParameters { get =>
        from h in _md.GetGenericParameters()
        select new JsGenericParameter(_reader, _reader.GetGenericParameter(h), _gc); }
}

class JsMethodSignature {
    MethodSignature<TType> _sig;

    public JsMethodSignature(MethodSignature<TType> sig) {
        _sig = sig;
    }

    public int GenericParameterCount { get => _sig.GenericParameterCount; }

    public JsSignatureHeader Header { get => new JsSignatureHeader(_sig.Header); }

    public IEnumerable<TType> ParameterTypes { get => _sig.ParameterTypes; }

    public int RequiredParameterCount { get => _sig.RequiredParameterCount; }

    public TType ReturnType { get => _sig.ReturnType; }
}

class JsSignatureHeader {
    SignatureHeader _sh;

    public JsSignatureHeader(SignatureHeader sh) {
        _sh = sh;
    }

    public IEnumerable<string> Attributes { get => _sh.Attributes.ToString().Split(", "); }

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

    public IEnumerable<string> Attributes { get => _pa.Attributes.ToString().Split(", "); }

    public IEnumerable<JsCustomAttribute> CustomAttributes { get =>
        from h in _pa.GetCustomAttributes()
        select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h)); }

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

    public IEnumerable<string> Attributes { get {
        if (_mi.Attributes == MethodImportAttributes.None)
            yield return "None";
        if (_mi.Attributes.HasFlag(MethodImportAttributes.ExactSpelling))
            yield return "ExactSpelling";
        if ((_mi.Attributes & MethodImportAttributes.BestFitMappingMask) == MethodImportAttributes.BestFitMappingEnable)
            yield return "BestFitMappingEnable";
        if ((_mi.Attributes & MethodImportAttributes.BestFitMappingMask) == MethodImportAttributes.BestFitMappingDisable)
            yield return "BestFitMappingDisable";
        if ((_mi.Attributes & MethodImportAttributes.CharSetMask) == MethodImportAttributes.CharSetAnsi)
            yield return "CharSetAnti";
        if ((_mi.Attributes & MethodImportAttributes.CharSetMask) == MethodImportAttributes.CharSetUnicode)
            yield return "CharSetUnicode";
        if ((_mi.Attributes & MethodImportAttributes.CharSetMask) == MethodImportAttributes.CharSetAuto)
            yield return "CharSetAuto";
        if ((_mi.Attributes & MethodImportAttributes.ThrowOnUnmappableCharMask) == MethodImportAttributes.ThrowOnUnmappableCharEnable)
            yield return "ThrowOnUnmappableCharEnable";
        if ((_mi.Attributes & MethodImportAttributes.ThrowOnUnmappableCharMask) == MethodImportAttributes.ThrowOnUnmappableCharDisable)
            yield return "ThrowOnUnmappableCharDisable";
        if (_mi.Attributes.HasFlag(MethodImportAttributes.SetLastError))
            yield return "SetLastError";
        if ((_mi.Attributes & MethodImportAttributes.CallingConventionMask) == MethodImportAttributes.CallingConventionWinApi)
            yield return "CallingConventionWinApi";
        if ((_mi.Attributes & MethodImportAttributes.CallingConventionMask) == MethodImportAttributes.CallingConventionCDecl)
            yield return "CallingConventionCDecl";
        if ((_mi.Attributes & MethodImportAttributes.CallingConventionMask) == MethodImportAttributes.CallingConventionStdCall)
            yield return "CallingConventionStdCall";
        if ((_mi.Attributes & MethodImportAttributes.CallingConventionMask) == MethodImportAttributes.CallingConventionThisCall)
            yield return "CallingConventionThisCall";
        if ((_mi.Attributes & MethodImportAttributes.CallingConventionMask) == MethodImportAttributes.CallingConventionFastCall)
            yield return "CallingConventionFastCall";
    } }

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

    public IEnumerable<JsCustomAttribute> CustomAttributes { get =>
        from h in _mr.GetCustomAttributes()
        select new JsCustomAttribute(_reader, _reader.GetCustomAttribute(h)); }
}

class MetadataPrinter {
    public static void usage() {
        Console.WriteLine("winmd-printer [-h] [-o output.json] input.winmd");
    }

    public static void Main(string[] args) {
        string? input = null;
        string? output = null;

        for (int i = 0; i < args.Length; ++i) {
            switch (args[i]) {
                case "-h":
                    usage();
                    return;
                case "-o":
                    output = args[++i];
                    break;
                default:
                    input = args[i];
                    break;
            }
        }

        if (input is null) {
            usage();
            return;
        }

        using var fs = new FileStream(input, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var pe = new PEReader(fs);
        var reader = pe.GetMetadataReader(MetadataReaderOptions.None);
        using var writer = (output is null) ? Console.Out : new StreamWriter(output);
        writer.Write(JsonSerializer.Serialize(
            from h in reader.TypeDefinitions
            select new JsTypeDefinition(reader, reader.GetTypeDefinition(h)),
            new JsonSerializerOptions { WriteIndented = true}));
    }
}
