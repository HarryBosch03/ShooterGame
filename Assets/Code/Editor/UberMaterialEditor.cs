using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Bosch.Editor
{
    public sealed class UberMaterialEditor : ShaderGUI
    {
        private Material material;

        public void MaterialProperty<T>(string name, System.Func<string, T> get, System.Action<string, T> set,
            System.Func<T, T> editor)
        {
            var val = get(name);

            EditorGUI.BeginChangeCheck();
            val = editor(val);
            if (!EditorGUI.EndChangeCheck()) return;
            
            Undo.RecordObject(material, "Changed Material Properties");
            set(name, val);
        }

        public void Div()
        {
            var rect = EditorGUILayout.GetControlRect(false);
            rect.y += rect.height * 0.5f;
            rect.height = 1.0f;
            EditorGUI.DrawRect(rect, new Color(1.0f, 1.0f, 1.0f, 0.1f));
        }

        public void MaterialPropertySlider(string name, string label)
        {
            MaterialProperty(name,
                n => new Vector2(material.GetFloat(n + "_Min"), material.GetFloat(n + "_Max")),
                (n, v) =>
                {
                    material.SetFloat(n + "_Min", v.x);
                    material.SetFloat(n + "_Max", v.y);
                },
                v =>
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        float x = Mathf.Round(v.x * 100.0f) / 100.0f, y = Mathf.Round(v.y * 100.0f) / 100.0f;
                        var rect = EditorGUILayout.GetControlRect(true);
                        var r = rect;
                        var labelWidth = EditorGUIUtility.labelWidth;

                        r.width = labelWidth;
                        GUI.Label(r, label);
                        rect.width -= labelWidth;
                        rect.x += labelWidth;
                        r = rect;


                        const float floatFieldSize = 40.0f;
                        const float padding = 15.0f;

                        r.width = floatFieldSize;
                        x = EditorGUI.FloatField(r, x);

                        r.x += r.width + padding;
                        r.width = rect.width - (floatFieldSize + padding) * 2.0f;
                        EditorGUI.MinMaxSlider(r, ref x, ref y, 0.0f, 1.0f);

                        r.x += r.width + padding;
                        r.width = floatFieldSize;
                        y = EditorGUI.FloatField(r, y);

                        return new Vector2(x, y);
                    }
                });
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            material = materialEditor.target as Material;
            if (!material) return;

            GUILayout.Label("Surface Properties");

            MaterialProperty("_MainTex", material.GetTexture, material.SetTexture,
                t => (Texture)EditorGUILayout.ObjectField("Main Texture", t, typeof(Texture2D), false));

            MaterialProperty("_Color", material.GetColor, material.SetColor,
                c => EditorGUILayout.ColorField("Main Color", c));

            MaterialPropertySlider("_Metallic", "Metallic");
            MaterialPropertySlider("_Roughness", "Roughness");

            MaterialProperty<int>("_Triplanar", material.GetInt, material.SetInt,
                v => EditorGUILayout.Toggle("Use Tri-planar", v == 1) ? 1 : 0);

            Div();
            GUILayout.Label("Noise Settings");
        }
    }
}