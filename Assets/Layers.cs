using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Description of various layers used in LiquidFab
 * 1 << 3 - Terrain
 * 1 << 4 - Water
 * 1 << 5 - UI
 * 1 << 6 - Buildings
 * 1 << 7 - Floaters
 * 1 << 8 - Buildings no floater collision
 * 
 * Terrain has two colliders - one for normal behavior, Terrain, one for clicking and finding out where to place
 * buildings, TerrainWithWater
 * 
 * Several invariants have to be preserved, notably:
 * - all buildings are either Buildings or BuildingsNoFloater
 */

public class ColliderLayers
{
    public const float Depth = 200; // no idea if this is reasonable, but let's use a common constant

    public const int Terrain = 1 << 3;
    public const int Water = 1 << 4;
    public const int UI = 1 << 5;
    public const int Buildings = 1 << 6;
    public const int Floaters = 1 << 7;
    public const int BuildingsNoFloater = 1 << 8;

    public static int AllBuildings
    {
        get => Buildings | BuildingsNoFloater;
    }
}