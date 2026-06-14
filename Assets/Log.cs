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
            var frac = Mathf.Min(1, ((wl - height) + 0.5f) / logHeight);
            rb.AddForce(new Vector3(0, 12 * frac, 0), ForceMode.Force);
            // flow
            rb.AddForce(new Vector3(terrain.s.waterFlowX[(int)x + ((int)y) * Terrain.TERRAIN_SIZE] * 0.3f, 0,
                terrain.s.waterFlowY[(int)x + ((int)y) * Terrain.TERRAIN_SIZE] * 0.3f), ForceMode.Force);
            // friction
            rb.AddForce(-rb.velocity * 3 / 4 * frac, ForceMode.Force);
            rb.AddTorque(-rb.angularVelocity * 3 / 4 * frac, ForceMode.Force);
        }
    }
}
