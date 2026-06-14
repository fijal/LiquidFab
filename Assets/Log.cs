using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Log : MonoBehaviour
{
    public Terrain terrain;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    void FixedUpdate()
    {
        var logHeight = 3.45f * 0.15f;
        var x = transform.position.x / Terrain.SCALE;
        var y = transform.position.z / Terrain.SCALE;
        var height = Mathf.Max(0, transform.position.y - terrain.heightFloat(x, y));
        var wl = terrain.water.waterLevelFloat(x, y); //[(int)x + (int)y * Terrain.TERRAIN_SIZE];
        if (wl - height > 0)
        {
            var rb = GetComponent<Rigidbody>();
            var frac = Mathf.Clamp01((wl - height) / logHeight);    // 0: water up to the middle; 1: water up to the top
            frac = 0.5f + 0.5f * frac;    // remap to the interval from 0.5 to 1
            var buoyancy = 11f;           // slightly bigger than g=9.81
            var bottom_direction = transform.up;
            if (bottom_direction.y > 0)
                bottom_direction = -bottom_direction;
            // flow
            var flowForce = new Vector3(terrain.s.waterFlowX[(int)x + ((int)y) * Terrain.TERRAIN_SIZE], buoyancy,
                terrain.s.waterFlowY[(int)x + ((int)y) * Terrain.TERRAIN_SIZE]) * 3;
            rb.AddForceAtPosition((flowForce - rb.velocity) * frac * 3f/4,
                transform.position + bottom_direction * Mathf.Lerp(logHeight, 0f, frac),
                ForceMode.Force);
        }
    }
}
