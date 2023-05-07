using System;
using UnityEngine.InputSystem;

namespace Bosch
{
    public static class Utility
    {
        public static class Input
        {
            public static string Device { get; set; }
            public static InputActionAsset InputAsset { get; set; }

            public static InputAction Bind(string binding, Action<bool> callback)
            {
                var action = new InputAction(binding: $"<{Device}>/{binding}");
                action.Enable();
                action.performed += ctx => callback(ctx.ReadValueAsButton());
                return action;
            }

            public static InputAction BindFromAsset(string name, Action<InputAction.CallbackContext> callback)
            {
                var action = InputAsset.FindAction(name);
                if (callback != null) action.performed += callback;
                return action;
            }

            public static InputAction BindFromAsset(string name) => BindFromAsset(name, (Action<InputAction.CallbackContext>)null);
            public static InputAction BindFromAsset(string name, Action callback) => BindFromAsset(name, (InputAction.CallbackContext ctx) => callback());
            public static InputAction BindFromAsset(string name, Action<bool> callback) => BindFromAsset(name, (InputAction.CallbackContext ctx) => callback(ctx.ReadValueAsButton()));
        }
    }
}