namespace Fundament.Capstone.Compiler

type Id = uint64

type ElementSize =
    | Empty = 0
    | Bit = 1
    | Byte = 2
    | TwoBytes = 3
    | FourBytes = 4
    | EightBytes = 5
    | Pointer = 6
    | InlineComposite = 7

type Value =
    | Void
    | Bool of bool
    | Int8 of int8
    | Int16 of int16
    | Int32 of int32
    | Int64 of int64
    | UInt8 of uint8
    | UInt16 of uint16
    | UInt32 of uint32
    | UInt64 of uint64
    | Float32 of float32
    | Float64 of float
    | Text of string
    | Data of byte array
    | List of obj
    | Enum of uint16
    | Struct of obj
    | Interface
    | AnyPointer of obj

type Type =
    | Void
    | Bool
    | Int8
    | Int16
    | Int32
    | Int64
    | UInt8
    | UInt16
    | UInt32
    | UInt64
    | Float32
    | Float64
    | Text
    | Data
    | List of Type
    | Enum of TypeId: Id * Brand: Brand
    | Struct of TypeId: Id * Brand: Brand
    | Interface of TypeId: Id * Brand: Brand
    | AnyPointer of AnyPointerType

and Brand = { Scopes: BrandScope list }

and BrandScope =
    { ScopeId: Id
      Variant: BrandScopeVariant }

and BrandScopeVariant =
    | Bind of BrandBinding list
    | Inherit

and BrandBinding =
    | Unbound
    | Type of Type

and AnyPointerType =
    | UnconstrainedAnyKind
    | UnconstrainedStruct
    | UnconstrainedList
    | UnconstrainedCapability
    | Parameter of Id: Id * ParameterIndex: uint16
    | ImplicitMethodParameter of ParameterIndex: uint16

type Annotation = { Id: Id; Brand: Brand; Value: Value }

type Field =
    { Name: string
      CodeOrder: uint16
      Annotations: Annotation list
      DiscriminantValue: uint16
      Variant: FieldVariant
      Ordinal: FieldOrdinal }

and FieldVariant =
    | Slot of Offset: uint32 * Type: Type * DefaultValue: Value * HadExplicitDefault: bool
    | Group of TypeId: Id

and FieldOrdinal =
    | Implicit
    | Explicit of uint16

type Enumerant =
    { Name: string
      CodeOrder: uint16
      Annotations: Annotation list }

type Superclass = { Id: Id; Brand: Brand }

type Method =
    { Name: string
      CodeOrder: uint16
      ImplicitParameters: string list
      ParamStructType: Id
      ParamBrand: Brand
      ResultStructType: Id
      ResultBrand: Brand
      Annotations: Annotation list }

type Node =
    { Id: Id
      Name: string
      DisplayName: string
      DisplayNamePrefixLength: uint32
      ParentId: Id
      Parameters: string list
      IsGeneric: bool
      Annotations: Annotation list
      Variant: NodeVariant }

and NodeVariant =
    | File
    | Struct of
        DataWordCount: uint16 *
        PointerCount: uint16 *
        IsGroup: bool *
        DiscriminantCount: uint16 *
        DiscriminantOffset: uint32 *
        Fields: Field list
    | Enum of Enumerant list
    | Interface of Methods: Method list * Superclasses: Superclass list
    | Const of Type: Type * Value: Value
    | Annotation of
        Type: Type *
        // Can an annotation target multiple things at once? Or can this be compressed into an enum in the object model?
        TargetsFile: bool *
        TargetsConst: bool *
        TargetsEnum: bool *
        TargetsEnumerant: bool *
        TargetsStruct: bool *
        TargetsField: bool *
        TargetsUnion: bool *
        TargetsGroup: bool *
        TargetsInterface: bool *
        TargetsMethod: bool *
        TargetsParam: bool *
        TargetsAnnotation: bool
