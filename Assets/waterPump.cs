using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class waterPump : MonoBehaviour
{
    float pos = 0;
    public float basePos = 0;

    public float fuelLevel;
    public ParticleSystem smoke;
    public const float MAX_FUEL = 180;

    void FixedUpdate()
    {
        if (fuelLevel > 0)
        {
            fuelLevel -= Time.fixedDeltaTime;
            if (fuelLevel <= 0)
            {
                fuelLevel = 0;
                smoke.Stop();
            }

            pos += Time.fixedDeltaTime;
            var c = transform.position;
            transform.position = new Vector3(c.x, basePos + 0.05f * (Mathf.Sin(pos * 3f) - 1), c.z);
        }
    }

    public void interact(Controls controls)
    {
        var g = Instantiate(controls.detailsPanel);
        g.GetComponent<DetailsPanel>().controls = controls;
        g.GetComponent<DetailsPanel>().interactable = gameObject;
        controls.detailsPanelActive = true;
    }

    public void feedFuel()
    {
        smoke.Play();
        fuelLevel = MAX_FUEL;
    }
}
