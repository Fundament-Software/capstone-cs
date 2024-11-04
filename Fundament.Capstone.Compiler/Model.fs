module Fundament.Capstone.Compiler.Model

open Capnp.Schema
open FSharpPlus

let inline private TODO<'T> m : 'T =
    raise (System.NotImplementedException(m))

let inline private BLOCKED_ON<'T> m : 'T =
    let message = sprintf "Blocked on: %s" (String.concat "\n" m)
    raise (System.NotImplementedException(message))

let inline private NOTDONE<'T> : 'T = raise (System.NotImplementedException())

let inline private outOfRange<'T> e : 'T = raise (System.ArgumentOutOfRangeException($"Unknown enum value: %d{(e)}"))

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

    static member Read(reader: Capnp.Schema.Value.READER) =
        match reader.which with
        | Value.WHICH.Void -> Void
        | Value.WHICH.Bool -> Bool(reader.Bool)
        | Value.WHICH.Int8 -> Int8(reader.Int8)
        | Value.WHICH.Int16 -> Int16(reader.Int16)
        | Value.WHICH.Int32 -> Int32(reader.Int32)
        | Value.WHICH.Int64 -> Int64(reader.Int64)
        | Value.WHICH.Uint8 -> UInt8(reader.Uint8)
        | Value.WHICH.Uint16 -> UInt16(reader.Uint16)
        | Value.WHICH.Uint32 -> UInt32(reader.Uint32)
        | Value.WHICH.Uint64 -> UInt64(reader.Uint64)
        | Value.WHICH.Float32 -> Float32(reader.Float32)
        | Value.WHICH.Float64 -> Float64(reader.Float64)
        | Value.WHICH.Text -> Text(reader.Text)
        | Value.WHICH.Data -> Data(reader.Data |> Array.ofSeq)
        | Value.WHICH.List -> List(reader.List)
        | Value.WHICH.Enum -> Enum(reader.Enum)
        | Value.WHICH.Struct -> NOTDONE
        | Value.WHICH.Interface -> Interface
        | Value.WHICH.AnyPointer -> NOTDONE
        | x -> outOfRange (int32 x)

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
    
    static let rec ReadImpl(reader: Capnp.Schema.Type.READER) =
        match reader.which with
        | Type.WHICH.Void -> Void
        | Type.WHICH.Bool -> Bool
        | Type.WHICH.Int8 -> Int8
        | Type.WHICH.Int16 -> Int16
        | Type.WHICH.Int32 -> Int32
        | Type.WHICH.Int64 -> Int64
        | Type.WHICH.Uint8 -> UInt8
        | Type.WHICH.Uint16 -> UInt16
        | Type.WHICH.Uint32 -> UInt32
        | Type.WHICH.Uint64 -> UInt64
        | Type.WHICH.Float32 -> Float32
        | Type.WHICH.Float64 -> Float64
        | Type.WHICH.Text -> Text
        | Type.WHICH.Data -> Data
        | Type.WHICH.List -> List(ReadImpl(reader.List.ElementType))
        | Type.WHICH.Enum -> Enum(reader.Enum.TypeId, Brand.Read(reader.Enum.Brand))
        | Type.WHICH.Struct -> Struct(reader.Struct.TypeId, Brand.Read(reader.Struct.Brand))
        | Type.WHICH.Interface -> Interface(reader.Interface.TypeId, Brand.Read(reader.Interface.Brand))
        | Type.WHICH.AnyPointer -> AnyPointer(AnyPointerType.Read(reader.AnyPointer))
        | x -> outOfRange (int32 x)
    
    static member Read(reader: Capnp.Schema.Type.READER) = ReadImpl reader

/// Corresponds to the "anyPointer" union variant in Type in the schema.
/// "anyPointer" is itself a union, which contains an "unconstrained" union variant that we've flattened here.
and AnyPointerType =
    | UnconstrainedAnyKind
    | UnconstrainedStruct
    | UnconstrainedList
    | UnconstrainedCapability
    | Parameter of Id: Id * ParameterIndex: uint16
    | ImplicitMethodParameter of ParameterIndex: uint16

    static member Read(reader: Capnp.Schema.Type.anyPointer.READER) =
        match reader.which with
        | Type.anyPointer.WHICH.Unconstrained -> 
            match reader.Unconstrained.which with
            | Type.anyPointer.unconstrained.WHICH.AnyKind -> UnconstrainedAnyKind
            | Type.anyPointer.unconstrained.WHICH.Struct -> UnconstrainedStruct
            | Type.anyPointer.unconstrained.WHICH.List -> UnconstrainedList
            | Type.anyPointer.unconstrained.WHICH.Capability -> UnconstrainedCapability
            | x -> outOfRange (int32 x)
        | Type.anyPointer.WHICH.Parameter -> Parameter(reader.Parameter.ScopeId, reader.Parameter.ParameterIndex)
        | Type.anyPointer.WHICH.ImplicitMethodParameter -> ImplicitMethodParameter(reader.ImplicitMethodParameter.ParameterIndex)
        | x -> outOfRange (int32 x)

and Brand = 
    { Scopes: BrandScope list}

    static member Read(reader: Capnp.Schema.Brand.READER) =
        { Scopes = reader.Scopes |> Seq.map BrandScope.Read |> List.ofSeq }

/// Corresponds to "Brand.Scope" in the schema 
and BrandScope =
    { ScopeId: Id
      Variant: BrandScopeVariant }
    static member Read(reader: Capnp.Schema.Brand.Scope.READER) =
        { ScopeId = reader.ScopeId; Variant = BrandScopeVariant.Read reader }

/// Corresponds to the union inside "Brand.Scope" in the schema 
and BrandScopeVariant =
    | Bind of BrandBinding list
    | Inherit

    static member Read(reader: Capnp.Schema.Brand.Scope.READER) =
        match reader.which with
        | Brand.Scope.WHICH.Bind -> Bind(reader.Bind |> Seq.map BrandBinding.Read |> List.ofSeq)
        | Brand.Scope.WHICH.Inherit -> Inherit
        | x -> outOfRange (int32 x)

/// Corresponds to "Brand.Binding" in the schema 
and BrandBinding =
    | Unbound
    | Type of Type

    static member Read(reader: Capnp.Schema.Brand.Binding.READER) =
        match reader.which with
        | Brand.Binding.WHICH.Unbound -> Unbound
        | Brand.Binding.WHICH.Type -> Type(Type.Read reader.Type)
        | x -> outOfRange (int32 x)

type Annotation = 
    { Id: Id; Brand: Brand; Value: Value }

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
      DisplayName: string
      DisplayNamePrefixLength: uint32
      Parameters: string list
      IsGeneric: bool
      Annotations: Annotation list
      Variant: NodeVariant }

    static let Read nameTable (reader: Capnp.Schema.Node.READER) =
        let readParameters (readers: Capnp.Schema.Node.Parameter.READER seq) = 
            readers |> Seq.map (fun reader -> reader.Name) |> List.ofSeq;
        
        { Id = reader.Id;
        DisplayName = reader.DisplayName;
        DisplayNamePrefixLength = reader.DisplayNamePrefixLength;
        Parameters = readParameters reader.Parameters;
        IsGeneric = reader.IsGeneric;
        Annotations = [ ]; // TODO: Actually implement this
        Variant = NodeVariant.Read nameTable reader }

and NodeVariant =
    | File
    | Struct of
        Name: string *
        DataWordCount: uint16 *
        PointerCount: uint16 *
        IsGroup: bool *
        DiscriminantCount: uint16 *
        DiscriminantOffset: uint32 *
        Fields: Field list
    | Enum of Name: string * Enumerant list
    | Interface of Name: string * Methods: Method list * Superclasses: Superclass list
    | Const of Name: string * Type: Type * Value: Value
    | Annotation of
        Name: string *
        Type: Type *
        // TODO: Can an annotation target multiple things at once? Or can this be compressed into an enum in the object model?
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

    static member Read nameTable (reader: Capnp.Schema.Node.READER) =
        let name = lazy (Map.find reader.Id nameTable)
        
        // TODO: We only deserilize the first "layer" of data right now. Implement deserialization functions for the lower types.
        match reader.which with
        | Node.WHICH.File -> File
        | Node.WHICH.Struct ->
            let structReader = reader.Struct
            Struct (
                name.Value,
                structReader.DataWordCount,
                structReader.PointerCount,
                structReader.IsGroup,
                structReader.DiscriminantCount,
                structReader.DiscriminantOffset,
                [])
        | Node.WHICH.Enum -> Enum(name.Value, [])
        | Node.WHICH.Interface -> Interface(name.Value, [], [])
        | Node.WHICH.Const -> Const(name.Value, )

let buildModel (reader: CodeGeneratorRequest.READER) =
    // Answers the question "What is the name of the node with this Id"
    let nameTable =
        reader.Nodes
        |> Seq.collect (fun nodeReader -> nodeReader.NestedNodes)
        |> fold (fun table nnr -> Map.add nnr.Id nnr.Name table) Map.empty

    let nodeTable = reader.Nodes |> Seq.map (fun reader -> reader.Id, reader) |> Map

    // Answers the question "What are the children of the node with this Id"
    let childrenTable =
        let foldFn table (reader: Capnp.Schema.Node.READER) =
            let parentId = reader.ScopeId
            let childrenIds = Map.tryFind parentId table |> Option.defaultValue [ reader.Id ]
            Map.add reader.ScopeId childrenIds table

        Seq.fold foldFn Map.empty reader.Nodes

    // Recursively builds a tree of nodes rooted at the give id
    // let buildTree id =

    ()
