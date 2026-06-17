using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Dialog : MonoBehaviour
{
    public TMP_Text text;
    public Terrain terrain;
    public int x, y;

    public void Update()
    {
        if (terrain == null)
            return;
        terrain.updateDialog(x, y);
    }

}
