// Author: leonlyd
// Contact: [leonliuyd@outlook.com]
// Created: 2024-01-09
// Last Modified: 2024-01-10
// Description: Unity3D的Addresable，易用性功能增加代码，此代码为Inspector的额外显示，为功能的主要入口，提供Group显示，Label显示和增减。
// Description in English by AI :Enhancements to the usability features of Unity3D's Addressables, this code serves as an additional display in the Inspector, acting as the main entry point for the functionality. It provides display, addition, and removal of Groups and Labels.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.Linq;
using static UnityEngine.EventSystems.EventTrigger;
using Codice.CM.Common.Replication;
using Unity.VisualScripting.IonicZip;
using UnityEngine.AddressableAssets;
using Unity.VisualScripting;
using System.Xml;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
[InitializeOnLoad]
public static class AddressableHelperHeaderGUI
{
    public struct LabelInfo
    {
        public string label;
        public int num;
    }
    static AddressableHelperHeaderGUI()
    {
        dicGUIDEntry = new Dictionary<String, AddressableAssetEntry>();
        dicOrignalLabelInfo = new Dictionary<string, int>();
        dicTargetLabelInfo = new Dictionary<string, int>();

        settings = AddressableAssetSettingsDefaultObject.Settings;

        Editor.finishedDefaultHeaderGUI += OnDefaultHeaderGUI;
    }
    static string newLabelName;
    static AddressableAssetSettings settings;
    static Dictionary<String, AddressableAssetEntry> dicGUIDEntry;
    static Dictionary<string, int> dicOrignalLabelInfo;
    static Dictionary<string, int> dicTargetLabelInfo;
    static bool showGroup = true;
    static float fadeGroupValue;

    static void BindAddresableSettings()
    {
        Selection.selectionChanged += SelectionChanged;
        AddressableAssetSettingsDefaultObject.Settings.OnModification += (A, B, C) => SelectionChanged();
    }
    static void SelectionChanged()
    {
        newLabelName = "";
        fadeGroupValue = 1.0f;
        dicGUIDEntry.Clear();
        dicOrignalLabelInfo.Clear();

        if (Selection.assetGUIDs == null || Selection.assetGUIDs.Count() == 0)
        {
            return;
        }

        foreach (var GUID in Selection.assetGUIDs)
        {
            AddressableAssetEntry entry = settings.FindAssetEntry(GUID);
            if (entry == null)
                continue;
            dicGUIDEntry[GUID] = entry;
        }

        var lables = settings.GetLabels();
        foreach (var label in lables)
        {
            dicOrignalLabelInfo[label] = 0;
        }

        foreach (var entry in dicGUIDEntry.Values)
        {
            foreach (var item in entry.labels)
            {
                dicOrignalLabelInfo[item] += 1;
            }
        }

        dicTargetLabelInfo = new Dictionary<string, int>(dicOrignalLabelInfo);
    }
    // Start is called before the first frame update
    private static void OnDefaultHeaderGUI(Editor editor)
    {
        if (settings == null)
        {
            Debug.LogError("Can't Get Addresable Setting");
            if (GUILayout.Button("No Addresable Settings , Click To Create", GUILayout.ExpandWidth(false)))
            {
                settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
                BindAddresableSettings();
            }
            return;
        }

        if (dicGUIDEntry.Count == 0)
            return;
        if (editor.targets.Count() != dicGUIDEntry.Count)
            return;

        GUIStyle style = GUI.skin.textField;
        var groupNames = dicGUIDEntry.Values.Select(entry => entry.parentGroup.name).Distinct();
        var labels = dicGUIDEntry.Values.SelectMany(entry => entry.labels).Distinct();

        showGroup = EditorGUILayout.ToggleLeft("Show Group and Labels", showGroup);

        // Start the fade group
        if (showGroup && EditorGUILayout.BeginFadeGroup(fadeGroupValue))
        {
            EditorGUI.indentLevel++;
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Group:" , GUILayout.Width(60) ,GUILayout.ExpandWidth(false));
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            foreach (var group in groupNames)
            {
                EditorGUILayout.LabelField(group, style, GUILayout.ExpandWidth(false));
            }
            GUILayout.EndVertical();
            if (GUILayout.Button("Select"))
            {
                AddresableGroupSelectWindow.OpenWindow(settings, dicGUIDEntry.Values.ToArray(), SelectionChanged);
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Label:" , GUILayout.Width(60), GUILayout.ExpandWidth(false));
            EditorGUILayout.BeginVertical(style);
            var defaltlabels = settings.GetLabels();
            var keys = new List<string>(dicTargetLabelInfo.Keys);
            foreach (var label in keys)
            {
                if (dicTargetLabelInfo[label] == dicGUIDEntry.Count || dicTargetLabelInfo[label] == 0)
                    EditorGUI.showMixedValue = false;
                else
                    EditorGUI.showMixedValue = true;

                var originalValue = dicTargetLabelInfo[label] != 0;
                var curValue = EditorGUILayout.ToggleLeft(label, originalValue, style);
                if (EditorGUI.showMixedValue == true)
                {
                    if (curValue)
                        dicTargetLabelInfo[label] = dicGUIDEntry.Count;
                }
                else
                {
                    dicTargetLabelInfo[label] = curValue ? dicGUIDEntry.Count : 0;
                }
            }
            EditorGUI.showMixedValue = false;
            EditorGUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            newLabelName = EditorGUILayout.TextField("New Label Name:", newLabelName);
            if (GUILayout.Button("Add"))
            {
                settings.AddLabel(newLabelName);
                EditorUtility.SetDirty(settings);
            }
            GUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(50);
            if (GUILayout.Button("Apply", GUILayout.Width(100)))
            {
                foreach (var value in dicTargetLabelInfo)
                {
                    if (value.Value == 0)
                    {
                        var entries = new List<AddressableAssetEntry>(dicGUIDEntry.Values);
                        foreach (var entry in entries)
                        {
                            entry.SetLabel(value.Key, false);
                        }
                    }
                    else if (value.Value == dicGUIDEntry.Count)
                    {
                        var entries = new List<AddressableAssetEntry>(dicGUIDEntry.Values);
                        foreach (var entry in entries)
                        {
                            entry.SetLabel(value.Key, true);
                        }
                    }
                }
                EditorUtility.SetDirty(settings);
            }
            GUILayout.Label("");
            if (GUILayout.Button("Revert" , GUILayout.Width(100)))
            {
                dicTargetLabelInfo = new Dictionary<string, int>(dicOrignalLabelInfo);
            }
            GUILayout.Space(50);
            EditorGUILayout.EndHorizontal();
            EditorGUI.indentLevel--;
        }
        EditorGUILayout.EndFadeGroup();
    }

    //private static AddressableAssetEntry GetAddressableAsset(UnityEngine.Object obj)
    //{
    //    // �������Ƿ���Addressable��Դ
    //    string assetPath = AssetDatabase.GetAssetPath(obj);
    //    if (string.IsNullOrEmpty(assetPath) == true)
    //        return null;
    //    // ��ȡ��ǰ��Ŀ�� Addressable ��Դ����
    //    AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
    //    string guid;
    //    long localID;
    //    // ʹ�� FindAssetEntry ����������Դ��Ŀ
    //    if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out localID) == false)
    //        return null;
    //    AddressableAssetEntry entry = settings.FindAssetEntry(guid);
    //    return entry;
    //}
}
