using Bosch.AI.Enemies.Core;

namespace Bosch.AI.BehaviourTree.Core
{
    public class BtSequence<T> : BtBranch<T> where T : Enemy
    {
        public BtSequence(string name, BTree<T> tree) : base(name, tree) { }

        protected override ExecutionResult OnExecute()
        {
            foreach (var child in children)
            {
                if (child.Execute() == ExecutionResult.Failure) return ExecutionResult.Failure;
            }

            return ExecutionResult.Success;
        }
    }
}