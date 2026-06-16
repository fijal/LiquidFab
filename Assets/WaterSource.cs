using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterSource : MonoBehaviour
{
    public float amount;

    const float SCALE = 3;

    public void increase(float amount)
    {
        this.amount += amount;
        var curScale = gameObject.transform.localScale;
        gameObject.transform.localScale = new Vector3(curScale.x, curScale.y + amount * SCALE, curScale.z);
    }

    public bool decrease(float amount)
    {
        this.amount -= amount;
        if (this.amount < 0)
            return false;
        else
        {
            var curScale = gameObject.transform.localScale;
            gameObject.transform.localScale = new Vector3(curScale.x, curScale.y - amount * SCALE, curScale.z);
        }
        return true;
    }
}
