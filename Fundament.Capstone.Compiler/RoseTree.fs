namespace Fundament.Capstone.Compiler

/// Defines an aribrary multi-way tree
/// This is an eager rose tree, so unlike Haskell's Data.Tree, this cannot be infinite.
type RoseTree<'T> = { Root: 'T; Children: RoseForest<'T> }

and RoseForest<'T> = RoseTree<'T> list

module RoseTree =
    let rec foldTree<'T, 'State> (folder: 'T -> 'State list -> 'State) (tree: RoseTree<'T>) =
        match tree with
        | { Root = root; Children = [] } -> folder root []
        | { Root = root; Children = children } -> folder root (List.map (foldTree folder) children)
