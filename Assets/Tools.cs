using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public interface ITool
{
    public Sprite getGrayIcon();
    public Sprite getColorIcon();
    public GameObject getRedGhost();
    public void activate(GameObject highlight);
    public void deactivate(GameObject highlight);
    public void hoverOverTerrain(GameObject highlight, GameObject camera, Terrain terrain);
    public void click(GameObject highlight, GameObject camera, Terrain terrain);
    public void rotate(GameObject highlight, float amount);
}

public class Receipe
{
    public Dictionary<ItemType, int> inputs;
    public Dictionary<ItemType, int> outputs;
    public float time;

    public Receipe(Dictionary<ItemType, int> inputs, Dictionary<ItemType, int> outputs, float time)
    {
        this.inputs = inputs;
        this.outputs = outputs;
        this.time = time;
    }
}

/*[CustomEditor(typeof(Receipe))]
public class ReceipeEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}*/

public class Tools : MonoBehaviour
{
    public Dictionary<string, ITool> allTools;
    public Dictionary<BuildingKind, ITool> buildingMapping; // BuildingKind -> Tool
    
    void Start()
    {
        allTools = new Dictionary<string, ITool>();

        var forgeSpec = transform.Find("Forge").gameObject.GetComponent<BuildingSpec>();
        forgeSpec.buildingCost = new Dictionary<ItemType, int>();
        forgeSpec.buildingCost[ItemType.IronPlate] = 2;
        forgeSpec.buildingCost[ItemType.Gear] = 1;
        forgeSpec.receipes = new Receipe[3];
        forgeSpec.receipes[0] = new Receipe(new Dictionary<ItemType, int> { { ItemType.Iron, 1 } }, new Dictionary<ItemType, int> { { ItemType.IronPlate, 1 } },
                                            5.0f);
        forgeSpec.receipes[1] = new Receipe(new Dictionary<ItemType, int> { { ItemType.Copper, 1 } }, new Dictionary<ItemType, int> { { ItemType.CopperPlate, 1 } },
                                            5.0f);

        forgeSpec.receipes[2] = new Receipe(new Dictionary<ItemType, int> { { ItemType.Rock, 1 } }, new Dictionary<ItemType, int>(), 3f);

        allTools["Forge"] = new ForgeTool(forgeSpec);

        var minerSpec = transform.Find("Miner").gameObject.GetComponent<BuildingSpec>();
        allTools["Miner"] = new MinerTool(minerSpec);
        
        var assemblerSpec = transform.Find("Assembler").gameObject.GetComponent<BuildingSpec>();

        assemblerSpec.receipes = new Receipe[1];
        assemblerSpec.receipes[0] = new Receipe(new Dictionary<ItemType, int> { { ItemType.Gear, 1 }, { ItemType.IronPlate, 2 } },
            new Dictionary<ItemType, int> { { ItemType.IronPlate, 1 }, { ItemType.Gear, 2 } }, 2.0f);

        allTools["Assembler"] = new AssemblerTool(assemblerSpec);
        
        var ww = transform.Find("WaterWheel").gameObject;
        var waterWheelSpec = ww.GetComponent<BuildingSpec>();
        allTools["WaterWheel"] = new WaterWheelTool(waterWheelSpec, ww.GetComponent<WaterWheelSpec>());
        
        var fenceSpec = transform.Find("Fence").gameObject.GetComponent<BuildingSpec>();
        allTools["Fence"] = new FenceBehaviour(fenceSpec);

        var dismantleSpec = transform.Find("Dismantle").gameObject.GetComponent<Dismantle>();
        allTools["Dismantle"] = new DismantleTool(dismantleSpec);

        var waterPumpSpec = transform.Find("WaterPump").gameObject.GetComponent<BuildingSpec>();
        allTools["WaterPump"] = new WaterPumpTool(waterPumpSpec);

        allTools["Select"] = new SelectTool(transform.Find("Select").gameObject.GetComponent<Select>());

        buildingMapping = new Dictionary<BuildingKind, ITool>();
        buildingMapping[BuildingKind.assembler] = allTools["Assembler"];
        buildingMapping[BuildingKind.waterWheel] = allTools["WaterWheel"];
        buildingMapping[BuildingKind.forge] = allTools["Forge"];
        buildingMapping[BuildingKind.miner] = allTools["Miner"];
        buildingMapping[BuildingKind.fence] = allTools["Fence"];
        buildingMapping[BuildingKind.waterPump] = allTools["WaterPump"];
    }
}
