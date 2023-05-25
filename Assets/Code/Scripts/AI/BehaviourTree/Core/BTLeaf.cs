using Bosch.AI.Enemies.Core;

namespace Bosch.AI.BehaviourTree.Core
{
    public abstract class BtLeaf<T> where T : Enemy
    {
        public string name;
        public readonly BTree<T> tree;

        protected BtLeaf(string name, BTree<T> tree)
        {
            this.tree = tree;
        }

        public ExecutionResult Execute()
        {
            var res = OnExecute();
            tree.callstack.Add(new BTree<T>.CallstackEntry(this, res));
            return res;
        }
        
        protected abstract ExecutionResult OnExecute();

        public enum ExecutionResult
        {
            Success,
            Failure,
        }
    }
}