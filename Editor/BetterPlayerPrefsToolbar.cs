using BetterPlayerPrefs.Runtime;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;

namespace BetterPlayerPrefs.Editor
{
    public static class BetterPlayerPrefsToolbar
    {
        private const string DialogTitle = "Clear PlayerPrefs?";
        private const string DialogMessage = "Are you sure you want to delete all PlayerPrefs? This cannot be undone.";

        [MainToolbarElement("BetterPlayerPrefs/Clear All", defaultDockPosition = MainToolbarDockPosition.Right)]
        public static MainToolbarElement ClearAllButton()
        {
            var icon = EditorGUIUtility.IconContent("SaveAs").image as Texture2D;
            var content = new MainToolbarContent("Clear PlayerPrefs", icon, "Clear all PlayerPrefs data.");

            return new MainToolbarButton(content, ClearAll);
        }

        private static void ClearAll()
        {
            if (!EditorUtility.DisplayDialog(DialogTitle, DialogMessage, "Yes", "No")) return;

            BetterPlayerPrefsManager.DeleteAll();
        }
    }
}
