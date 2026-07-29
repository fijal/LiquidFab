using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectTool : ITool
{
    Select spec;
    GameObject hover;
    GameObject selectedObject;

    const int MAX_INVENTORY_COUNT = 5;

    List<ItemType> inventory;

    public SelectTool(Select spec)
    {
        this.spec = spec;
    }

    public void activate(GameObject highlight)
    {
        inventory = new List<ItemType>();
    }

    void updateCursor()
    {
        var cursor = new Texture2D(48, 48, TextureFormat.RGBA32, false);
        cursor.alphaIsTransparency = true;
        cursor.CopyPixels(spec.baseCursor, 0, 0, 0, 0, 48, 48, 0, 0, 0);
        for (int i = 0; i < inventory.Count; ++i)
            Graphics.CopyTexture(spec.rock.texture, 0, 0, 0, 0, 16, 12, cursor, 0, 0, i * 5, 0);
        Cursor.SetCursor(cursor, new Vector2(0, 0), CursorMode.Auto);
    }

    void addInventoryItem(Item itemSpec, int pos)
    {
        var go = Object.Instantiate<GameObject>(spec.iconPrefab, spec.handInventory.transform);
        go.GetComponent<Image>().sprite = itemSpec.icon;
        go.transform.localPosition = new Vector3(10 + pos * 20, -20, 0);
        //spec.handInventory
    }

    void removeInventoryItem()
    {
        Object.Destroy(spec.handInventory.transform.GetChild(spec.handInventory.transform.childCount - 1).gameObject);
    }

    public void click(GameObject highlight, GameObject camera, Terrain terrain)
    {
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200, ColliderLayers.Floaters))
        {
            if (inventory.Count >= MAX_INVENTORY_COUNT)
                return;
            var tp = hit.transform.gameObject.GetComponent<Floater>().tp;
            inventory.Add(tp);
            terrain.removeFloater(hit.transform.gameObject);
            addInventoryItem(terrain.items.items[tp], inventory.Count - 1);
        }
        else if (inventory.Count > 0)
        {
            if (Physics.Raycast(ray, out hit, 200, ColliderLayers.Water))
            {
                var tp = inventory[inventory.Count - 1];
                inventory.RemoveAt(inventory.Count - 1);
                var loc = new Vector3(hit.point.x, terrain.heightWaterFloat(hit.point.x / Terrain.SCALE, hit.point.z / Terrain.SCALE) + 1.0f, hit.point.z);
                var itemSpec = terrain.items.items[tp];
                terrain.spawnFloater(loc, itemSpec.prefab, tp);
                removeInventoryItem();
            }
        }

    }

    public void deactivate(GameObject highlight)
    {
    }

    public Sprite getColorIcon()
    {
        throw new System.NotImplementedException();
    }

    public Sprite getGrayIcon()
    {
        throw new System.NotImplementedException();
    }

    public GameObject getRedGhost()
    {
        throw new System.NotImplementedException();
    }

    public void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain)
    {
        var ray = camera.GetComponent<Camera>().ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 200, ColliderLayers.Floaters))
        {
            var go = hit.transform.gameObject;
            if (go == selectedObject)
            {
                return;
            }
            if (hover != null)
                Object.Destroy(hover);
            hover = Object.Instantiate<GameObject>(spec.selectPrefab, go.transform);
            hover.transform.position = go.transform.position;
            hover.transform.rotation = Quaternion.Euler(90, 0, 0);
            selectedObject = go;
        } else
        {
            Object.Destroy(hover);
            selectedObject = null;
            hover = null;
        }
    }

    public void rotate(GameObject highlight, float amount)
    {
    }
}

public class Select : MonoBehaviour
{
    public GameObject selectPrefab, rockPrefab;
    public GameObject handInventory, iconPrefab;
    public Sprite rock;
    public Texture2D baseCursor;
}
