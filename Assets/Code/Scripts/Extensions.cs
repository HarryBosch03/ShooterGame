using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosch
{
    public static class Extensions
    {
        public static class NameComparisons
        {
            public delegate bool Delegate(string a, string b);

            public static string Simplify(string s) => s.Trim().ToLower().Replace(" ", "");

            public static bool Hard(string a, string b) => a == b;
            public static bool Soft(string a, string b) => Simplify(a) == Simplify(b);
        }

        public static List<Transform> DeepFindAll(this Component component, string name, NameComparisons.Delegate areEqual = null)
        {
            var res = new List<Transform>();
            component.DeepFind(name, t =>
            {
                res.Add(t);
                return false;
            }, areEqual);
            return res;
        }
        
        public static Transform DeepFind(this Component component, string name, NameComparisons.Delegate areEqual = null)
        {
            Transform res = null;
            component.DeepFind(name, t =>
            {
                res = t;
                return true;
            }, areEqual);
            return res;
        }
        
        private static bool DeepFind(this Component component, string name, Func<Transform, bool> callback, NameComparisons.Delegate areEqual = null)
        {
            var transform = component.transform;
            areEqual ??= NameComparisons.Soft;

            if (areEqual(transform.name, name) && callback(transform)) return true;
            foreach (Transform child in transform)
            {
                if (child.DeepFind(name, callback, areEqual)) return true;
            }
            
            return false;
        }

        public static bool State(this InputAction action) => action.ReadValue<float>() > 0.5f;

        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.TryGetComponent(out T component) ? component : gameObject.AddComponent<T>();
        }

        public static T Best<T>(this IEnumerable<T> list, Func<T, float> scoringCallback, float startingScore = float.MinValue, T fallback = default)
        {
            var best = fallback;
            var bestScore = startingScore;

            foreach (var element in list)
            {
                var score = scoringCallback(element);
                if (score < bestScore) continue;

                best = element;
                bestScore = score;
            }

            return best;
        }
    }
}