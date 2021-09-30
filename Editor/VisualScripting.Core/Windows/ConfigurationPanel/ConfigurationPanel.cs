using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Unity.VisualScripting
{
    public class ConfigurationPanel
    {
        public ConfigurationPanel(Product product)
        {
            Ensure.That(nameof(product)).IsNotNull(product);

            this.product = product;
            configurations = product.plugins.Select(plugin => plugin.configuration).ToList();
        }

        private readonly Product product;
        private readonly List<PluginConfiguration> configurations;
        private string label => product.configurationPanelLabel;

        public void PreferenceItem()
        {
            EditorGUIUtility.labelWidth = 220;
            OnGUI();
        }

        public IEnumerable<string> GetSearchKeywords()
        {
            List<string> keywords = new List<string>();
            foreach (var configuration in configurations)
            {
                if (configuration.Any(i => i.visible))
                {
                    foreach (var item in configuration.Where(i => i.visible))
                    {
                        keywords.Add(item.member.HumanName());
                    }
                }
            }

            return keywords;
        }

        public static class Styles
        {
            static Styles()
            {
                header = new GUIStyle(EditorStyles.boldLabel);
                header.fontSize = 14;
                header.margin = new RectOffset(2, 0, 15, 6);
                header.padding = new RectOffset(0, 0, 0, 0);

                tabBackground = new GUIStyle("ButtonMid");
                tabBackground.alignment = TextAnchor.UpperLeft;
                tabBackground.margin = new RectOffset(0, 0, 0, 0);
                tabBackground.padding = new RectOffset(7, 7, 7, 7);
                tabBackground.fixedHeight = 54;

                tabIcon = new GUIStyle();
                tabIcon.fixedWidth = tabIcon.fixedHeight = 24;
                tabIcon.margin = new RectOffset(0, 7, 2, 0);

                tabTitle = new GUIStyle(EditorStyles.label);
                tabTitle.padding = new RectOffset(0, 0, 0, 0);
                tabTitle.margin = new RectOffset(0, 0, 0, 0);
                tabTitle.normal.background = ColorPalette.transparent.GetPixel();
                tabTitle.onNormal.background = ColorPalette.transparent.GetPixel();
                tabTitle.normal.textColor = ColorPalette.unityForeground;
                tabTitle.onNormal.textColor = ColorPalette.unityForegroundSelected;

                tabDescription = new GUIStyle();
                tabDescription.wordWrap = true;
                tabDescription.fontSize = 10;
                tabDescription.margin = new RectOffset(0, 0, 0, 0);
                tabDescription.normal.background = ColorPalette.transparent.GetPixel();
                tabDescription.onNormal.background = ColorPalette.transparent.GetPixel();
                tabDescription.normal.textColor = ColorPalette.unityForegroundDim;
                tabDescription.onNormal.textColor = ColorPalette.unityForegroundSelected;
            }

            public const int iconSize = 12;

            public static readonly GUIStyle header;
            public static readonly GUIStyle tabBackground;
            public static readonly GUIStyle tabIcon;
            public static readonly GUIStyle tabTitle;
            public static readonly GUIStyle tabDescription;
        }

        #region Drawing

        static Vector2 scroll { get; set; }

        private static void Header(string text)
        {
            GUILayout.Label(text, Styles.header);
            LudiqGUI.Space(4);
        }

        private void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(EditorApplication.isCompiling);

            scroll = GUILayout.BeginScrollView(scroll);

            LudiqGUI.BeginHorizontal();
            LudiqGUI.BeginVertical();

            foreach (var configuration in configurations)
            {
                if (configuration.Any(i => i.visible))
                {
                    if (configurations.Count > 1)
                    {
                        Header(configuration.header.Replace(label + " ", ""));
                    }

                    EditorGUI.BeginChangeCheck();

                    using (Inspector.expandTooltip.Override(true))
                    {
                        foreach (var item in configuration.Where(i => i.visible))
                        {
                            LudiqGUI.Space(2);

                            LudiqGUI.BeginHorizontal();

                            LudiqGUI.Space(4);

                            var iconPosition = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.Width(Styles.iconSize), GUILayout.Height(Styles.iconSize), GUILayout.ExpandHeight(false), GUILayout.ExpandWidth(false));

                            EditorTexture icon = null;
                            string tooltip = null;

                            if (item is ProjectSettingMetadata)
                            {
                                icon = BoltCore.Icons.projectSetting;
                                tooltip = "Project Setting: Shared across users, local to this project. Included in version control.";
                            }
                            else if (item is EditorPrefMetadata)
                            {
                                icon = BoltCore.Icons.editorPref;
                                tooltip = "Editor Pref: Local to this user, shared across projects. Excluded from version control.";
                            }

                            if (icon != null)
                            {
                                using (LudiqGUI.color.Override(GUI.color.WithAlpha(0.6f)))
                                {
                                    GUI.Label(iconPosition, new GUIContent(icon[Styles.iconSize], tooltip), GUIStyle.none);
                                }
                            }

                            LudiqGUI.Space(6);

                            LudiqGUI.BeginVertical();

                            LudiqGUI.Space(-3);

                            LudiqGUI.InspectorLayout(item);

                            LudiqGUI.EndVertical();

                            LudiqGUI.EndHorizontal();
                        }
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        configuration.Save();
                        InternalEditorUtility.RepaintAllViews();
                    }
                }
            }

            LudiqGUI.Space(8);

            if (GUILayout.Button("Reset to Defaults"))
            {
                if (EditorUtility.DisplayDialog("Reset to Defaults", "Are you sure you want to reset your preferences and project settings to default?", "Reset"))
                {
                    foreach (var configuration in configurations)
                    {
                        configuration.Reset();
                        configuration.Save();
                    }

                    InternalEditorUtility.RepaintAllViews();
                }
            }

            LudiqGUI.Space(8);
            LudiqGUI.EndVertical();
            LudiqGUI.Space(8);
            LudiqGUI.EndHorizontal();
            GUILayout.EndScrollView();
            EditorGUI.EndDisabledGroup();
        }

        #endregion
    }
}
