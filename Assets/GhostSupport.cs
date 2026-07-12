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
        if (cur.GetComponent<MeshRenderer>())
        {
            var mats = new Material[cur.GetComponent<MeshRenderer>().sharedMaterials.Length];
            for (int j = 0; j < mats.Length; ++j)
                mats[j] = mat;
            cur.GetComponent<MeshRenderer>().sharedMaterials = mats;
        }

        for (int i = 0; i < cur.transform.childCount; ++i)
        {
            var c = cur.transform.GetChild(i).gameObject;
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