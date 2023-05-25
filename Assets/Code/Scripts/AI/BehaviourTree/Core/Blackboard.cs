using System.Collections.Generic;

namespace Bosch.AI.BehaviourTree.Core
{
    public class Blackboard
    {
        public readonly Dictionary<string, object> data = new();

        public T Get<T>(string key, T fallback = default)
        {
            if (data.ContainsKey(key)) return (T)data[key];
            return fallback;
        }

        public void Set<T>(string key, T value)
        {
            if (data.ContainsKey(key)) data.Add(key, value);
            else data[key] = value;
        }
    }
}