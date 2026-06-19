using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tooltip : MonoBehaviour
{
    public float fadeTimer;

    void FixedUpdate()
    {
        fadeTimer += Time.fixedDeltaTime;
        if (fadeTimer < 1)
            return;
        if (fadeTimer < 3)
        {
            var c = GetComponent<TMP_Text>().color;
            GetComponent<TMP_Text>().color = new Color(1, 1, 1, (3 - fadeTimer) / 2);
        } else
        {
            gameObject.SetActive(false);
        }
    }
}
