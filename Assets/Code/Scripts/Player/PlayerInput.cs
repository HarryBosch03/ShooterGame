using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bosch.Player
{
    [System.Serializable]
    public class PlayerInput
    {
        [SerializeField] private InputActionAsset inputAsset;

        private PlayerAvatar avatar;
        private Action updateEvent;

        public InputAxis2D Move { get; private set; }
        public InputWrapper Jump { get; private set; }
        public InputWrapper Slam { get; private set; }
        public InputWrapper Shoot { get; private set; }
        public InputWrapper Holster { get; private set; }
        public InputWrapper Slide { get; private set; }
        public List<InputWrapper> EquipWeapon { get; private set; }

        public void Initialize(PlayerAvatar avatar)
        {
            this.avatar = avatar;
            inputAsset.Enable();

            Move = BindAxis2D("Move");

            Jump = BindWrapper("Jump");
            Slam = BindWrapper("Slam");
            Shoot = BindWrapper("Shoot");
            Holster = BindWrapper("Holster");
            Slide = BindWrapper("Slide");

            EquipWeapon = new List<InputWrapper>();
            while (true)
            {
                var binding = BindWrapper($"Weapon.{EquipWeapon.Count + 1}");
                if (binding == null) break;
                EquipWeapon.Add(binding);
            }
        }

        public InputWrapper BindWrapper(string name)
        {
            var action = inputAsset.FindAction(name);
            if (action == null) return null;
            return new InputWrapper(action, ref updateEvent);
        }
        
        public InputAxis2D BindAxis2D(string name)
        {
            var action = inputAsset.FindAction(name);
            if (action == null) return null;
            return new InputAxis2D(action, ref updateEvent);
        }

        public void Update() => updateEvent?.Invoke();
        
        public class InputAxis2D
        {
            private Vector2 lastValue;
            private readonly InputActionReference actionRef;
            
            public Vector2 Value { get; set; }
            public Vector2 Delta { get; set; }

            public InputAxis2D(InputAction action, ref Action updateEvent)
            {
                updateEvent += Update;
                
                actionRef = ScriptableObject.CreateInstance<InputActionReference>();
                actionRef.Set(action);
            }
            
            private void Update()
            {
                lastValue = Value;
                Value = actionRef.action.ReadValue<Vector2>();
                Delta = Value - lastValue;
            }
        }

        public class InputWrapper
        {
            public float pressPoint = 0.5f;
            private float lastValue;
            private readonly InputActionReference actionRef;
            
            public float Value { get; set; }
            public bool Pressed => Value > pressPoint;
            public InputState State { get; private set; }

            public InputWrapper(InputAction action, ref Action updateEvent)
            {
                updateEvent += Update;
                
                actionRef = ScriptableObject.CreateInstance<InputActionReference>();
                actionRef.Set(action);
            }
            
            private void Update()
            {
                lastValue = Value;
                Value = actionRef.action.ReadValue<float>();

                var pressed = Value > pressPoint;
                var lPressed = lastValue > pressPoint;
                if (pressed)
                {
                    State = lPressed ? InputState.Pressed : InputState.PressedThisFrame;
                }
                else
                {
                    State = lPressed ? InputState.ReleasedThisFrame : InputState.Released;
                }
            }

            public void CallIfDown(Action callback)
            {
                if (State != InputState.PressedThisFrame) return;
                callback();
            }

            public enum InputState
            {
                Released,
                ReleasedThisFrame,
                Pressed,
                PressedThisFrame,
            }
        }
    }
}