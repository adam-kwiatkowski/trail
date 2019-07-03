using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class Selection //: ScriptableObject
{
    public List<Block> Blocks = new List<Block>();

    /*
    [MenuItem("Tools/MyTool/Do It in C#")]
    static void DoIt()
    {
        EditorUtility.DisplayDialog("MyTool", "Do It in C# !", "OK", "");
    }
    */
}