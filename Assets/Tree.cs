using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tree : MonoBehaviour
{
    public float age;
    const float MAX_AGE = 180;

    void Start()
    {
        
    }

    void Update()
    {
        age += Time.deltaTime;
        var scale = age / MAX_AGE * 0.1f + 0.02f;
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
