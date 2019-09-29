using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    public static MapManager GetMapManager() {
        return (MapManager) HushPuppy.safeFindComponent("GameController", "MapManager");
    }

    [Header("References")]
    [SerializeField]
    Tilemap seaTilemap;

    void Update() {
        var worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        if (Input.GetKeyDown(KeyCode.Z)) {
            print("worldPos: " + worldPos.ToString() + " => " + IsPositionValid(worldPos));
        }
    }

    public bool IsPositionValid(Vector3 position) {
        Vector3Int vec = HushPuppy.GetVecAsTileVec(position);

        TileBase seaTile = seaTilemap.GetTile(vec);
        bool insideSea = seaTile != null;

        return !insideSea;
    }
}
