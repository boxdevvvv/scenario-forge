using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace ScenarioForge.Editor
{
    /// <summary>Searchable menu of step types, grouped by the categories of <see cref="ScenarioStepMenuAttribute"/>.</summary>
    sealed class StepTypeDropdown : AdvancedDropdown
    {
        readonly string title;
        readonly Action<Type> onTypeSelected;

        public StepTypeDropdown(AdvancedDropdownState state, string title, Action<Type> onTypeSelected)
            : base(state)
        {
            this.title = title;
            this.onTypeSelected = onTypeSelected;
            minimumSize = new Vector2(340f, 320f);
        }

        /// <summary>Builds the menu tree. Exposed for tests.</summary>
        internal AdvancedDropdownItem BuildTree() => BuildRoot();

        /// <summary>The text shown for a type. AdvancedDropdown has no tooltips, so the tooltip follows the name.</summary>
        internal static string FormatItemName(StepTypeInfo info)
        {
            return string.IsNullOrEmpty(info.Tooltip) ? info.DisplayName : $"{info.DisplayName}  —  {info.Tooltip}";
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(title);
            var groups = new Dictionary<string, AdvancedDropdownItem>(StringComparer.Ordinal);

            foreach (StepTypeInfo info in StepTypeCatalog.GetStepTypes())
            {
                AdvancedDropdownItem parent = GetGroup(root, groups, info.Category);
                parent.AddChild(new TypeItem(info));
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is TypeItem typeItem)
                onTypeSelected?.Invoke(typeItem.Info.Type);
        }

        static AdvancedDropdownItem GetGroup(AdvancedDropdownItem root, Dictionary<string, AdvancedDropdownItem> groups, string category)
        {
            AdvancedDropdownItem parent = root;
            string path = string.Empty;
            foreach (string segment in category.Split('/'))
            {
                path = path.Length == 0 ? segment : path + "/" + segment;
                if (!groups.TryGetValue(path, out AdvancedDropdownItem group))
                {
                    group = new AdvancedDropdownItem(segment);
                    parent.AddChild(group);
                    groups[path] = group;
                }

                parent = group;
            }

            return parent;
        }

        internal sealed class TypeItem : AdvancedDropdownItem
        {
            public TypeItem(StepTypeInfo info)
                : base(FormatItemName(info))
            {
                Info = info;
            }

            public StepTypeInfo Info { get; }
        }
    }
}
