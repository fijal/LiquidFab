using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tree : MonoBehaviour
{
    public float age;
    public const float MAX_AGE = 180;
    const float MAX_SCALE = 1.5f;

    void Start()
    {
        
    }

    void Update()
    {
        age += Time.deltaTime;
        var scale = (age / MAX_AGE * 0.1f + 0.02f) / MAX_SCALE;
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
