﻿//
//  UIInputFieldEditor.cs
//  survive
//
//  Created by xingweizhen on 10/31/2017.
//
//

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ZFrame.UGUI
{
    [CustomEditor(typeof(UIInputField), true)]
    public class UIInputFieldEditor : TMPro.EditorUtilities.TMP_InputFieldEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            EditorGUILayout.Separator();
            EditorGUILayout.LabelField("Event Setting", EditorStyles.boldLabel);
            ++EditorGUI.indentLevel;
            EditorUtil.DrawInteractEvent(serializedObject.FindProperty("m_ValueChanged"), false);
            EditorUtil.DrawInteractEvent(serializedObject.FindProperty("m_Submit"), false);
            --EditorGUI.indentLevel;

            serializedObject.ApplyModifiedProperties();
        }
    }
}