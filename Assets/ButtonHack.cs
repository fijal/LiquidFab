using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHack : MonoBehaviour
{
    bool isClicked = false;
    public Building building;
    public int receipeIndex;

    public void disableButton()
    {
        var colors = GetComponent<Button>().colors;
        colors.normalColor = new Color((float)0x6A / 255, (float)0x6A / 255, (float)0x6A / 0xFF);
        colors.selectedColor = new Color((float)0x6A / 255, (float)0x6A / 255, (float)0x6A / 0xFF);
        GetComponent<Button>().colors = colors;
        building.receipesEnabled[receipeIndex] = false;
        isClicked = false;
    }

    public void enableButton()
    {
        var colors = GetComponent<Button>().colors;
        colors.normalColor = new Color((float)0x9A / 255, (float)0x85 / 255, (float)0xC / 0xFF);
        colors.selectedColor = new Color((float)0x9A / 255, (float)0x85 / 255, (float)0xC / 0xFF);
        GetComponent<Button>().colors = colors;
        building.receipesEnabled[receipeIndex] = true;
        isClicked = true;
    }

    public void clicked()
    {
        if (isClicked)
            disableButton();
        else
            enableButton();
        EventSystem.current.SetSelectedGameObject(null);
    }
}
