using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitFactory : MonoBehaviour
{
    public TileMap.MapController controller;

    public GameObject UnitPrefab;
    public List<UnitType> unitTypes = new List<UnitType>();
    public Unit DebugCreateUnit(string type, Vector3 position, string faction,(int,int) gatheringSpot,bool exactPosition = true)
    {
        GameObject unitObject = GameObject.Instantiate(UnitPrefab);
        unitObject.transform.position = position;
        unitObject.name = type;
        Unit unit = unitObject.GetComponent<Unit>();
        unit.unitMovement.currentTile = (Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
        UnitType unitType = FindUnitData(type);
        unit.unitType = unitType;
        if (unitType == null)
        {
            GameObject.Destroy(unitObject);
            Debug.LogError("Objective type not found");
            return null;
        }
        unit.MaxHP = unitType.maxHP;
        unit.speed = unitType.speed;
        unit.Faction = faction;
        foreach (ComponentWithParams comp in unitType.components)
        {
            unitObject.transform.Find(comp.name).gameObject.SetActive(true);
        }
        unit.controller = controller;
        unit.unitMovement.BeginPathfind(gatheringSpot);
        //controller.map.Occupy(controller.GetMapTileFromWorldPosition(position));
        return unit;
    }
    public UnitType FindUnitData(string type)
    {
        for (int i = 0; i < unitTypes.Count; i++)
        {
            if (type == unitTypes[i].type)
            {
                return unitTypes[i];
            }
        }
        return null;
    }
    public Unit CreatePlaceableUnit(string type, string faction)
    {
        GameObject unitObject = GameObject.Instantiate(UnitPrefab);
        unitObject.name = type;
        unitObject.transform.position = new Vector3(-10, -10);
        Unit unit = unitObject.GetComponent<Unit>();
        UnitType unitType = FindUnitData(type);
        unit.unitType = unitType;
        if (unitType == null)
        {
            GameObject.Destroy(unitObject);
            Debug.LogError("Objective type not found");
            return null;
        }
        unit.MaxHP = unitType.maxHP;
        unit.speed = unitType.speed;
        unit.Faction = faction;
        unit.range = unitType.range;
        unit.capturePower = unitType.capturePower;
        foreach (ComponentWithParams comp in unitType.components)
        {
            unitObject.transform.Find(comp.name).gameObject.SetActive(true);
        }
        unit.controller = controller;
        unit.unitMovement.gameObject.SetActive(true);
        unit.freezeLogic = true;
        return unit;
    }
    public Unit ReconstructUnitFromData(DataStorage unitData)
    {
        GameObject unitObject = GameObject.Instantiate(UnitPrefab);
        string unitName = unitData.FindParam("type").value;
        unitObject.name = unitName;
        unitObject.transform.position = new Vector3(-10, -10);
        Unit unit = unitObject.GetComponent<Unit>();
        UnitType unitType = FindUnitData(unitName);
        unit.unitType = unitType;
        if (unitType == null)
        {
            GameObject.Destroy(unitObject);
            Debug.LogError("Objective type not found");
            return null;
        }
        unit.MaxHP = unitType.maxHP;
        unit.speed = unitType.speed;
        unit.range = unitType.range;
        unit.capturePower = unitType.capturePower;
        unit.Faction = unitData.FindParam("faction").value;
        unit.HP = int.Parse(unitData.FindParam("hp").value);
        foreach (DataStorage comp in unitData.subcomponents)
        {
            unitObject.transform.Find(comp.name).gameObject.SetActive(true);
        }
        foreach (ComponentWithParams comp in unitType.components)
        {
            unitObject.transform.Find(comp.name).gameObject.SetActive(true);
        }
        unit.reconstructionData = unitData;
        unit.isReconstructed = true;
        unit.controller = controller;
        unit.unitMovement.gameObject.SetActive(true);
        unit.freezeLogic = true;
        return unit;
    }
}
[System.Serializable]
public class UnitType
{
    public string type;
    public int maxHP;
    public int speed;
    public int capturePower;
    public int range;
    public List<ComponentWithParams> components = new List<ComponentWithParams>();
    public List<UnitSprites> sprites = new List<UnitSprites>();
}
[System.Serializable]
public class UnitSprites
{
    public string name;
    public List<Sprite> stateSprite = new List<Sprite>();
    public float animSpeed = 3;
}
