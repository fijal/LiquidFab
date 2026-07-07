using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GhostSupport : MonoBehaviour
{
}

[CustomEditor(typeof(GhostSupport))]
public class GhostEditor : Editor
{
    public Material mat;

    void recursivelyFixMaterial(GameObject cur, Material mat)
    {
        for (int i = 0; i < cur.transform.childCount; ++i)
        {
            var c = cur.transform.GetChild(i).gameObject;
            if (c.GetComponent<MeshRenderer>())
            {
                var mats = new Material[c.GetComponent<MeshRenderer>().sharedMaterials.Length];
                for (int j = 0; j < mats.Length; ++j)
                    mats[j] = mat;
                c.GetComponent<MeshRenderer>().sharedMaterials = mats;
            }
            recursivelyFixMaterial(c, mat);
        }
    }

    public override void OnInspectorGUI()
    {
        mat = EditorGUILayout.ObjectField(mat, typeof(Material), true) as Material;
        if (GUILayout.Button("Fix the material"))
        {
            var t = (target as GhostSupport).gameObject;
            recursivelyFixMaterial(t, mat);
        }
    }
}