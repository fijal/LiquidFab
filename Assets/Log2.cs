using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Log2 : MonoBehaviour
{
    public Terrain terrain;
    public Vector2 force;
    Vector2 velocity;

    const float REPULSIVE_MULTIPLIER = 0.1f;
    const float REPULSIVE_CAP = 1f;
    const float FLOW_FORCE = 1f;

    void FixedUpdate()
    {
        var cur = transform.position;
        var x = cur.x / Terrain.SCALE;
        var y = cur.z / Terrain.SCALE;
        var flowX = terrain.water.flowX((int)x, (int)y);
        var flowY = terrain.water.flowY((int)x, (int)y);
        var flow = new Vector2(flowX, flowY) / Mathf.Max(0.05f, terrain.water.waterLevelFloat(x, y));
        var z = terrain.water.waterLevelFloat(x, y) + terrain.heightFloat(x, y);
        var flowForce = (flow * FLOW_FORCE - velocity);
        var repForce = force * REPULSIVE_MULTIPLIER;
        if (repForce.magnitude > REPULSIVE_CAP)
            repForce = repForce.normalized * REPULSIVE_CAP;
        velocity += (flowForce + repForce) * Time.fixedDeltaTime;
        var newPos = new Vector3(transform.position.x + velocity.x * Time.fixedDeltaTime, z,
            transform.position.z + velocity.y * Time.fixedDeltaTime);
        if (terrain.water.waterLevelFloat(newPos.x, newPos.z) < 0.01f)
        {
            velocity = flow * FLOW_FORCE;
            newPos = new Vector3(transform.position.x + velocity.x * Time.fixedDeltaTime, z,
            transform.position.z + velocity.y * Time.fixedDeltaTime);
        }

        /* when in free fall below the terrain or off to the sides */
        if (newPos.y < -1000)
            Destroy(gameObject);
        else
            transform.position = newPos;
    }
}
