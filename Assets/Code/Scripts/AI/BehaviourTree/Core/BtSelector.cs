using Bosch.AI.Enemies.Core;

namespace Bosch.AI.BehaviourTree.Core
{
    public class BtSelector<T> : BtBranch<T> where T : Enemy
    {
        public BtSelector(string name, BTree<T> tree) : base(name, tree) { }
        
        protected override ExecutionResult OnExecute()
        {
            foreach (var child in children)
            {
                if (child.Execute() == ExecutionResult.Success) return ExecutionResult.Success;
            }

            return ExecutionResult.Failure;
        }
    }
}