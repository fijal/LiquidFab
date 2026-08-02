using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tooltip : MonoBehaviour
{
    public float fadeTimer;
    GameObject text;

    private void Start()
    {
        text = transform.Find("Text").gameObject;
    }

    void FixedUpdate()
    {
        var cur = text.transform.localPosition;
        text.transform.localPosition = new Vector3(cur.x, cur.y + Time.fixedDeltaTime * 30, cur.z);
        fadeTimer += Time.fixedDeltaTime;
        if (fadeTimer < 1)
            return;
        if (fadeTimer < 3)
        {
            var c = text.GetComponent<TMP_Text>().color;
            text.GetComponent<TMP_Text>().color = new Color(1, 1, 1, (3 - fadeTimer) / 2);
        } else
            Destroy(gameObject);
    }
}
