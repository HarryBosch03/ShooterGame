using System.Collections.Generic;
using Bosch.AI.Enemies.Core;

namespace Bosch.AI.BehaviourTree.Core
{
    public abstract class BtBranch<T> : BtLeaf<T> where T : Enemy
    {
        public readonly List<BtLeaf<T>> children = new();

        protected BtBranch(string name, BTree<T> tree) : base(name, tree) { }

        public BtBranch<T> Add(params BtLeaf<T>[] children)
        {
            this.children.AddRange(children);
            return this;
        }
    }
}