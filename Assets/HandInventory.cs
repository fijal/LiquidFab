using UnityEngine;

public class HandInventory : MonoBehaviour
{
    void Update()
    {
        var t = transform as RectTransform;
        //t.localPosition = new Vector3(100, 100, 0);
        transform.position = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0);       
    }
}
