# Author: leonlyd
# Contact: [leonliuyd@outlook.com]
# Created: 2024-01-09
# Last Modified: 2024-01-10
# Description: Unity3D的Addresable，易用性功能增加代码，此代码为其他一个脚本的辅助类，用于打开一个窗口选择要被更改为的Group。
# Description in English by AI: Code enhancing the usability features of Unity3D's Addressables. This script acts as a helper class for another script, used for opening a window to select the Group to be changed.

using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using PlasticGui;
using static UnityEngine.EventSystems.EventTrigger;

public class AddresableGroupSelectWindow : EditorWindow
{
    static AddresableGroupSelectWindow instance;
    AddressableAssetSettings settings;
    AddressableAssetEntry[] entries;
    Action doneCallback;
    AddressableAssetGroup[] groups;
    int? selectedGroupIndex = null;
    Vector2 scrollPosition;
    Dictionary<AddressableAssetGroup, int> groupEntryCounts;

    public static void OpenWindow(AddressableAssetSettings settings, AddressableAssetEntry[] entries, Action done)
    {
        if (instance == null)
            instance = EditorWindow.CreateInstance<AddresableGroupSelectWindow>();

        instance.settings = settings;
        instance.entries = entries;
        instance.doneCallback = done;
        instance.InitGroups();
        instance.Show();
    }

    void InitGroups()
    {
        groups = settings.groups.ToArray();
        groupEntryCounts = new Dictionary<AddressableAssetGroup, int>();

        foreach (var group in groups)
        {
            // ����ÿ�� group �д���� entries �ж��ٸ�������� group
            groupEntryCounts[group] = entries.Count(e => e.parentGroup == group);
        }

        // ����Ƿ�������Ŀ������ͬһ��group
        if (entries.Length > 0 && entries.All(e => e.parentGroup == entries[0].parentGroup))
        {
            selectedGroupIndex = Array.IndexOf(groups, entries[0].parentGroup);
        }
    }

    void OnGUI()
    {
        GUILayout.Label("Select Addressable Group", EditorStyles.boldLabel);

        // ��ʼ������ͼ
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // ʹ��textField��ʽ����һ��������
        GUILayout.BeginVertical(GUI.skin.textField);

        foreach (var group in groups)
        {
            int entryCount = groupEntryCounts[group];

            GUIStyle toggleStyle = new GUIStyle("Button");
            if (group.ReadOnly)
            {
                toggleStyle.normal.textColor = Color.red;
                toggleStyle.onNormal.textColor = Color.red;
                toggleStyle.focused.textColor = Color.red;
                toggleStyle.onFocused.textColor = Color.red;
                toggleStyle.hover.textColor = Color.red;
                toggleStyle.onHover.textColor = Color.red;
            }

            string label = $"{group.Name} ({entryCount} entries)";

            using (new EditorGUI.DisabledScope(group.ReadOnly))
            {
                bool isSelected = GUILayout.Toggle(selectedGroupIndex == Array.IndexOf(groups, group), label, toggleStyle);

                if (isSelected && selectedGroupIndex != Array.IndexOf(groups, group))
                {
                    selectedGroupIndex = Array.IndexOf(groups, group);
                }
            }
        }

        // ����������͹�����ͼ
        GUILayout.EndVertical();
        GUILayout.EndScrollView();

        if (GUILayout.Button("Apply") && selectedGroupIndex.HasValue)
        {
            ApplyGroupToEntries();
        }
    }

    void ApplyGroupToEntries()
    {
        var selectedGroup = groups[selectedGroupIndex.Value];
        if (selectedGroup != null)
        {
            foreach (var entry in entries)
            {
                var oldGroup = entry.parentGroup;
                var newGroup = selectedGroup;
                settings.CreateOrMoveEntry(entry.guid, newGroup);
            }
            doneCallback?.Invoke();
        }
        EditorUtility.SetDirty(settings);
        Close();
    }
}