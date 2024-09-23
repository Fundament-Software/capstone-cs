namespace Fundament.Capstone.Compiler

open System.Runtime.CompilerServices

/// Defines an aribrary multi-way tree
/// This is an eager rose tree, so unlike Haskell's Data.Tree, this cannot be infinite.
type RoseTree<'T> = { Root: 'T; Children: RoseForest<'T> }

and RoseForest<'T> = RoseTree<'T> list
