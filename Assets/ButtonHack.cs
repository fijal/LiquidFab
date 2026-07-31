using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHack : MonoBehaviour
{
    bool isClicked = false;

    public void clicked()
    {
        if (isClicked)
        {
            var colors = GetComponent<Button>().colors;
            colors.normalColor = new Color((float)0x6A / 255, (float)0x6A / 255, (float)0x6A / 0xFF);
            colors.selectedColor = new Color((float)0x6A / 255, (float)0x6A / 255, (float)0x6A / 0xFF);
            GetComponent<Button>().colors = colors;
            isClicked = false;
        }
        else
        {
            var colors = GetComponent<Button>().colors;
            colors.normalColor = new Color((float)0x9A / 255, (float)0x85 / 255, (float)0xC / 0xFF);
            colors.selectedColor = new Color((float)0x9A / 255, (float)0x85 / 255, (float)0xC / 0xFF);
            GetComponent<Button>().colors = colors;
            isClicked = true;
        }
        EventSystem.current.SetSelectedGameObject(null);
    }
}
