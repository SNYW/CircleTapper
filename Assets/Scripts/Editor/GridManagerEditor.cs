using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor(typeof(InWorldGridManager))]
    public class GridManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            InWorldGridManager gridManager = (InWorldGridManager)target;
            if (GUILayout.Button("Generate Grid"))
            {
                gridManager.InitGrid();
            }
            if (GUILayout.Button("Clear Grid"))
            {
                GridManager.Dispose();
            }
        }
    }
}