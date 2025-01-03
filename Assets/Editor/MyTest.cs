using System;
using ScenePerfomanceTool;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    public class MyTest:EditorWindow
    {
        static MyTest Instance;
        
        [MenuItem("Tools/MyTest")]
        static void GetStatic()
        {
            if (Instance==null)
            {
                Instance = ScriptableObject.CreateInstance<MyTest>();
            }
            Instance.Show();
        }

        private void OnGUI()
        {
            if (GUILayout.Button("获取数据"))
            {
                GameObject go = Selection.activeGameObject;
                HotmapData hm = go.GetComponent<HotmapData>();
                if (hm != null)
                {
                    Debug.Log(hm.GetData());
                }
            }

            if (GUILayout.Button("测试一下序列化采样点数据"))
            {
                
            }
        }
    }
}