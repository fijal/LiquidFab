using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

    public string getHelperText()
    {
        return "";
    }

    public void activate(GameObject highlight)
    {
        inventory = new List<ItemType>();
    }

    public void deactivate(GameObject highlight)
    {
        for (int i = 0; i < inventory.Count; ++i)
            removeInventoryItem();
    }

    void addInventoryItem(Item itemSpec, int pos)
    {
        var go = Object.Instantiate<GameObject>(spec.iconPrefab, spec.handInventory.transform);
        go.GetComponent<Image>().sprite = itemSpec.icon;
        go.transform.localPosition = new Vector3(10 + pos * 40, -40, 0);
        //spec.handInventory
    }

    void removeInventoryItem()
    {
        Object.Destroy(spec.handInventory.transform.GetChild(spec.handInventory.transform.childCount - 1).gameObject);
    }

    public void click(GameObject highlight, GameObject camera, Terrain terrain, bool modifier=false)
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
        } else if (Physics.Raycast(ray, out hit, ColliderLayers.Depth, ColliderLayers.AllBuildings)) {
            if (inventory.Count == 0)
                terrain.controls.showBuildingMenu(hit.transform.gameObject);
            else
            {
                var b = hit.transform.GetComponent<Building>();
                var c = hit.transform.GetComponent<Construction>();
                var tp = inventory[inventory.Count - 1];
                if (b != null)
                {
                    // XXX can this be done through an interface?
                    if (!b.isValidIngredient(tp))
                        terrain.controls.showTooltip("Not a valid ingredient");
                    else
                    {
                        removeInventoryItem();
                        inventory.RemoveAt(inventory.Count - 1);
                        b.addInventory(tp);
                    }
                } else
                {
                    if (!c.isValidIngredient(tp))
                        terrain.controls.showTooltip("Not a valid construction item");
                    else
                    {
                        removeInventoryItem();
                        inventory.RemoveAt(inventory.Count - 1);
                        c.addInventory(tp, terrain);
                    }
                }
            }
        }
        else if (inventory.Count > 0)
        {
            if (Physics.Raycast(ray, out hit, 200, ColliderLayers.Water))
            {
                var tp = inventory[inventory.Count - 1];
                inventory.RemoveAt(inventory.Count - 1);
                var loc = new Vector3(hit.point.x, terrain.heightWaterFloat(hit.point.x / Terrain.SCALE, hit.point.z / Terrain.SCALE) + 1.0f, hit.point.z);
                terrain.spawnFloater(loc, tp);
                removeInventoryItem();
            }
        } else
        {
            //Debug.Log("clicked nothing");
            
        }

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

        // XXX this logic is all slightly wrong and for some reason hoverPrefab has strange axis, but I gonna ignore it
        var mask = ColliderLayers.Floaters | ColliderLayers.AllBuildings;
        if (Physics.Raycast(ray, out hit, ColliderLayers.Depth, mask))
        {
            var go = hit.transform.gameObject;
            if (go == selectedObject)
            {
                return;
            }
            if (hover != null)
                Object.Destroy(hover);
            hover = Object.Instantiate<GameObject>(spec.selectPrefab, go.transform);
            var cur = go.transform.position;
            // XXX we need a better way to answer the question "how big is this building", for now a bunch of heuristics
            if (go.GetComponent<MeshFilter>() != null)
            {
                var mesh = go.GetComponent<MeshFilter>().sharedMesh;
                var size = Mathf.Max(mesh.bounds.extents.x, mesh.bounds.extents.z);
                hover.transform.localScale = new Vector3(size * 4, size * 4, size * 4);
            } else if (go.GetComponent<BoxCollider>() != null) {
                var x = go.GetComponent<BoxCollider>().bounds.extents;
                var size = Mathf.Max(x.x, x.y, x.z);
                hover.transform.localScale = new Vector3(size * 4, size * 4, size * 4);
            }
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
}
