using UnityEditor;
using UnityEngine;

namespace NekoSune.Worlds.Editor
{
    internal static class NekoStyles
    {
        static GUIStyle _headerTitle;
        static GUIStyle _subtitle;
        static GUIStyle _card;
        static GUIStyle _cardTitle;
        static GUIStyle _cardDescription;
        static GUIStyle _primaryButton;

        public static GUIStyle HeaderTitle { get { Ensure(); return _headerTitle; } }
        public static GUIStyle Subtitle { get { Ensure(); return _subtitle; } }
        public static GUIStyle Card { get { Ensure(); return _card; } }
        public static GUIStyle CardTitle { get { Ensure(); return _cardTitle; } }
        public static GUIStyle CardDescription { get { Ensure(); return _cardDescription; } }
        public static GUIStyle PrimaryButton { get { Ensure(); return _primaryButton; } }

        public static void Ensure()
        {
            if (_headerTitle != null) return;

            _headerTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 20,
                wordWrap = true
            };

            _subtitle = new GUIStyle(EditorStyles.wordWrappedMiniLabel)
            {
                wordWrap = true
            };

            _card = new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 4, 4)
            };

            _cardTitle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13
            };

            _cardDescription = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                wordWrap = true
            };

            _primaryButton = new GUIStyle(GUI.skin.button)
            {
                fontStyle = FontStyle.Bold,
                fixedHeight = 28f
            };
        }

        public static void Header(string title, string subtitle)
        {
            Ensure();
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label(title, HeaderTitle);
            GUILayout.Label(subtitle, Subtitle);
            EditorGUILayout.EndVertical();
        }
    }
}
