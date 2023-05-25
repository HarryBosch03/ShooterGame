using System.Collections.Generic;
using Bosch.AI.Enemies.Core;

namespace Bosch.AI.BehaviourTree.Core
{
    [System.Serializable]
    public class BTree<T> where T : Enemy
    {
        public readonly T target;
        public BtLeaf<T> entry;
        public readonly List<BtLeaf<T>> leaves;

        public readonly List<CallstackEntry> callstack = new();
        
        public BTree(T target, BtLeaf<T> entry)
        {
            this.target = target;
            this.entry = entry;

            leaves = new List<BtLeaf<T>>();
            leaves.Add(entry);
        }

        [System.Serializable]
        public class CallstackEntry
        {
            private BtLeaf<T> leaf;
            private BtLeaf<T>.ExecutionResult result;

            public CallstackEntry(BtLeaf<T> leaf, BtLeaf<T>.ExecutionResult result)
            {
                this.leaf = leaf;
                this.result = result;
            }
        }
    }
}