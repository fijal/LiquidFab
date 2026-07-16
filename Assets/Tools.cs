using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public interface ITool
{
    public Sprite getGrayIcon();
    public Sprite getColorIcon();
    public void activate(GameObject highlight);
    public void deactivate(GameObject highlight);
    public void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain);
    public void clickTerrain(GameObject highlight, Terrain terrain, Vector3 hitPoint);
    public void rotate(GameObject highlight, float amount);
}

public class Tools : MonoBehaviour
{
    public Dictionary<string, ITool> allTools;
    
    void Start()
    {
        allTools = new Dictionary<string, ITool>();

        var forgeSpec = transform.Find("Forge").gameObject.GetComponent<BuildingSpec>();
        allTools["Forge"] = new ForgeTool(forgeSpec);

        var minerSpec = transform.Find("Miner").gameObject.GetComponent<BuildingSpec>();
        allTools["Miner"] = new MinerTool(minerSpec);
        
        var assemblerSpec = transform.Find("Assembler").gameObject.GetComponent<BuildingSpec>();
        allTools["Assembler"] = new AssemblerTool(assemblerSpec);
        
        var ww = transform.Find("WaterWheel").gameObject;
        var waterWheelSpec = ww.GetComponent<BuildingSpec>();
        allTools["WaterWheel"] = new WaterWheelBehaviour(waterWheelSpec, ww.GetComponent<WaterWheelSpec>());
        
        var fenceSpec = transform.Find("Fence").gameObject.GetComponent<BuildingSpec>();
        allTools["Fence"] = new FenceBehaviour(fenceSpec);
    }
}
