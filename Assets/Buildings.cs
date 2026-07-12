using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Buildings : MonoBehaviour
{
    public Material greenTransparent, redTransparent;
    
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

    void Start()
    {
        for (int i = 0; i < transform.childCount; ++i)
        {
            var spec = transform.GetChild(i).gameObject.GetComponent<BuildingSpec>();
            //var go = PrefabUtility.InstantiatePrefab(spec.prefab);
            //spec.greenPrefab = go;
            //recursivelyFixMaterial(go, greenTransparent);
            //go.SetActive(false);
        }
    }
}
