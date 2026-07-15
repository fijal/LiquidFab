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
        var forgeSpec = transform.Find("Forge").gameObject.GetComponent<BuildingSpec>();
        forgeSpec.behaviour = new ForgeBehaviour(forgeSpec);
        var minerSpec = transform.Find("Miner").gameObject.GetComponent<BuildingSpec>();
        minerSpec.behaviour = new BuildingFreePlacement(minerSpec);
        var assemblerSpec = transform.Find("Assembler").gameObject.GetComponent<BuildingSpec>();
        assemblerSpec.behaviour = new BuildingFreePlacement(assemblerSpec);
        var ww = transform.Find("WaterWheel").gameObject;
        var waterWheelSpec = ww.GetComponent<BuildingSpec>();
        waterWheelSpec.behaviour = new WaterWheelBehaviour(waterWheelSpec, ww.GetComponent<WaterWheelSpec>());
    }
}
