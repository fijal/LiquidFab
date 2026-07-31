using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingDetails : MonoBehaviour
{
    public Image icon;
    public GameObject receipePrefab, itemPrefab;

    const float RECEIPE_GAP = 100;
    const int INGREDIENT_SIZE = 64;

    int populateIngredients(Dictionary<ItemType, int> ingredients, GameObject parent, Items items, int x)
    {
        var l = new ItemType[ingredients.Count];
        int i = 0;
        foreach (var k in ingredients.Keys)
        {
            l[i] = k;
            i++;
        }
        System.Array.Sort(l);
        for (i = 0; i < l.Length; i++)
        {
            var item = Instantiate(itemPrefab, parent.transform);
            (item.transform as RectTransform).anchoredPosition = new Vector2(x, (item.transform as RectTransform).anchoredPosition.y);
            item.GetComponent<Image>().sprite = items.items[l[i]].icon;
            x += INGREDIENT_SIZE;
        }
        return x;
    }

    public void populateReceipes(Receipe[] receipes, Items items)
    {
        for (int i = 0; i < receipes.Length; i++)
        {
            var r = Instantiate(receipePrefab, transform.Find("Panel"));
            var b = r.transform.localPosition.y;
            r.transform.localPosition = new Vector3(r.transform.localPosition.x, b - i * RECEIPE_GAP, 1);
            var rec = receipes[i];
            var x = populateIngredients(rec.inputs, r, items, 0);
            var arrow = r.transform.Find("Arrow") as RectTransform;
            arrow.anchoredPosition = new Vector3(x, arrow.anchoredPosition.y);
            x += (int)arrow.rect.xMax;
            populateIngredients(rec.outputs, r, items, x);
        }
    }
}
