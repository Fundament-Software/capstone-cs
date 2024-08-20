namespace Fundament.Capstone.Compiler

type Id = uint64

type ElementSize =
    | Zero
    | One
    | Byte
    | TwoBytes
    | FourBytes
    | EightBytes
    | Pointer
    | InlineComposite

type Field = { name: string }

type Parameter = { name: string }

type Annotation = { Id: Id }

type NodeKind =
    | File
    | Struct of
        DataWordCount: uint16 *
        PointerCount: uint16 *
        PreferredListEncoding: ElementSize *
        IsGroup: bool *
        DiscriminantCount: uint16 *
        DiscriminantOffset: uint32 *
        Fields: Field list
// | Enum of Enumerants list
// | Interface of Methods list * Superclasses list
// | Const of Type: Type * Value: Value
// | Annotation of Type: Type * Target: Target Set

type Node =
    { Id: Id
      Name: string
      DisplayName: string
      DisplayNamePrefixLength: uint32
      ParentId: Id
      Parameters: Parameter list
      Annotations: Annotation list
      Kind: NodeKind }
