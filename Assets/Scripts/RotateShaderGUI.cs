#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class RotateShaderGUI : ShaderGUI
{
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // Находим свойства
        MaterialProperty mainTex = FindProperty("_MainTex", properties);
        MaterialProperty rotationAngle = FindProperty("_RotationAngle", properties);
        MaterialProperty useRotation = FindProperty("_UseRotation", properties);
        MaterialProperty rotateCW90 = FindProperty("_RotateCW90", properties);
        MaterialProperty rotate180 = FindProperty("_Rotate180", properties);

        // Отображаем свойства
        materialEditor.ShaderProperty(mainTex, mainTex.displayName);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rotation Settings", EditorStyles.boldLabel);

        // Включение/отключение вращения
        materialEditor.ShaderProperty(useRotation, useRotation.displayName);

        if (useRotation.floatValue > 0.5f)
        {
            // Показываем слайдер для угла
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            materialEditor.ShaderProperty(rotationAngle, rotationAngle.displayName);

            // Визуальный слайдер
            float angle = rotationAngle.floatValue;
            angle = EditorGUILayout.Slider("Angle (Degrees)", angle, 0, 360);
            rotationAngle.floatValue = angle;

            // Кнопки для быстрых значений
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("0°")) rotationAngle.floatValue = 0;
            if (GUILayout.Button("90°")) rotationAngle.floatValue = 90;
            if (GUILayout.Button("180°")) rotationAngle.floatValue = 180;
            if (GUILayout.Button("270°")) rotationAngle.floatValue = 270;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            // Предупреждение если используются старые тогглы
            if (rotateCW90.floatValue > 0.5f || rotate180.floatValue > 0.5f)
            {
                EditorGUILayout.HelpBox("Deprecated rotation toggles are active. Disable them to use custom angle rotation.", MessageType.Warning);
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Legacy Settings", EditorStyles.miniBoldLabel);
        EditorGUILayout.HelpBox("These are deprecated. Use 'Rotation Angle' instead.", MessageType.Info);

        materialEditor.ShaderProperty(rotateCW90, rotateCW90.displayName);
        materialEditor.ShaderProperty(rotate180, rotate180.displayName);

        if (rotateCW90.floatValue > 0.5f && rotate180.floatValue > 0.5f)
        {
            EditorGUILayout.HelpBox("Both legacy toggles active. Result: 270° rotation.", MessageType.Info);
        }
    }
}
#endif