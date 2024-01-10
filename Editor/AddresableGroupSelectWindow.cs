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
            // 计算每个 group 中传入的 entries 有多少个属于这个 group
            groupEntryCounts[group] = entries.Count(e => e.parentGroup == group);
        }

        // 检查是否所有条目都属于同一个group
        if (entries.Length > 0 && entries.All(e => e.parentGroup == entries[0].parentGroup))
        {
            selectedGroupIndex = Array.IndexOf(groups, entries[0].parentGroup);
        }
    }

    void OnGUI()
    {
        GUILayout.Label("Select Addressable Group", EditorStyles.boldLabel);

        // 开始滚动视图
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // 使用textField样式创建一个包含框
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

        // 结束包含框和滚动视图
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