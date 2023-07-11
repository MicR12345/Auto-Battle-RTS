using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml.Serialization;
public class Objective : MonoBehaviour,Selectable,Placeable,StoresData,Damageable,Targetable,AIControllable,Captureable
{
    public TileMap.MapController controller;

    public GameObject selectorObject;
    public Animator animator;

    public ObjectiveGraphics graphics;

    public (int, int) gatherSpot = (-1, -1);
    private int maxHP;

    public List<StoresData> componentSerializableData = new List<StoresData>();
    public DataStorage reconstructionData = null;
    public bool isReconstructed= false;
    public bool freezeLogic = false;

    public bool destructible = false;
    IEnumerator captureCoorutine;
    [SerializeField]
    List<CaptureProgress> captureProgresses = new List<CaptureProgress>();
    string graphicsState = "";
    public int MaxHP
    {
        get { return maxHP; }
        set 
        {
            maxHP = value;
            HP = value; 
        }
    }
    [SerializeField]
    private int hp;
    public int HP
    {
        get { return hp; }
        set {
            if (value <= 0)
            {
                if (faction!="Neutral")
                {
                    if (destructible)
                    {
                        DestroyThis();
                        return;
                    }
                    NeutralizeThis();
                }
                else
                {
                    hp = maxHP / 2;
                }
                //TODO : Change to neutral
            }
            else
            {
                if (faction != "Neutral")
                {
                    hp = value;
                    ResolveGraphics();
                }
                else
                {
                    ResolveGraphics();
                }
            }
        }
    }
    IEnumerator CaputureCoorutine()
    {
        while (faction=="Neutral")
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 20);
            for (int i = 0; i < colliders.Length; i++)
            {
                Unit unit;
                if (colliders[i].TryGetComponent<Unit>(out unit))
                {
                    CaptureProgress progress = FindCaptureProgress(unit.Faction);
                    if (progress != null)
                    {
                        progress.value += unit.capturePower;
                        CheckForCapture();
                    }
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }
    void CheckForCapture()
    {
        if (faction == "Neutral")
        {
            foreach (CaptureProgress progress in captureProgresses)
            {
                if (progress.value>= maxHP)
                {
                    CaptureThis(progress.faction);
                    return;
                }
            }
        }
    }

    CaptureProgress FindCaptureProgress(string faction)
    {
        foreach (CaptureProgress progress in captureProgresses)
        {
            if (progress.faction == faction)
            {
                return progress;
            }
        }
        return null;
    }
    public void TryIncrementingCaptureProgress(int value,string faction)
    {
        if (this.faction=="Neutral")
        {
            CaptureProgress progress = FindCaptureProgress(faction);
            if (progress!=null)
            {
                progress.value += value/2;
                CheckForCapture();
            }
            else
            {
                captureProgresses.Add(new CaptureProgress(0,faction));
            }
        }
    }
    void CaptureThis(string faction)
    {
        freezeLogic = false;
        this.faction = faction;
        if (faction==controller.aiController.controlledFaction)
        {
            controller.aiController.RegisterObjective(this);
        }
        StopCoroutine(captureCoorutine);
    }
    
    void NeutralizeThis()
    {
        if (faction == controller.aiController.controlledFaction)
        {
            controller.aiController.UnregisterObjective(this);
        }
        faction = "Neutral";
        freezeLogic = true;
        captureCoorutine = CaputureCoorutine();
        StartCoroutine(captureCoorutine);
    }
    void DestroyThis()
    {
        //TODO
    }
    public string faction;

    void Selectable.OnSelect()
    {
        selectorObject.SetActive(true);
    }

    void Selectable.OnAction(Vector3 screenPosition)
    {
        Vector3 pos = Camera.main.ScreenToWorldPoint(screenPosition);
        gatherSpot = controller.GetMapTileFromWorldPosition(pos);
    }

    void Selectable.Unselect()
    {
        selectorObject.SetActive(false);
    }

    void Placeable.Place(Vector3 position)
    {
        transform.position = position;
        controller.objectiveFactory.OccupySpaceUnderObjective(controller.GetMapTileFromWorldPosition(position));
        controller.RegisterObjective(this);
        if (controller.aiController.controlledFaction==faction)
        {
            controller.aiController.RegisterObjective(this);
        }
        if (faction!="Neutral")
        {
            freezeLogic = false;
        }
        else
        {
            captureCoorutine = CaputureCoorutine();
            StartCoroutine(CaputureCoorutine());
        }
        
    }

    void Placeable.Discard()
    {
        GameObject.Destroy(gameObject);
    }

    DataStorage StoresData.GetData()
    {
        return GenerateData();
    }
    DataStorage GenerateData()
    {
        (int, int) postion = controller.GetMapTileFromWorldPosition(transform.position);
        DataStorage dataStorage = new DataStorage("Objective");
        dataStorage.RegisterNewParam("name", transform.name);
        dataStorage.RegisterNewParam("x", postion.Item1.ToString());
        dataStorage.RegisterNewParam("y", postion.Item2.ToString());
        dataStorage.RegisterNewParam("gatherSpotX", gatherSpot.Item1.ToString());
        dataStorage.RegisterNewParam("gatherSpotY", gatherSpot.Item2.ToString());
        dataStorage.RegisterNewParam("maxHP", maxHP.ToString());
        dataStorage.RegisterNewParam("hp", HP.ToString());
        dataStorage.RegisterNewParam("faction", faction);
        dataStorage.RegisterNewParam("graphicsPackage", graphics.objectiveName);
        foreach (StoresData item in componentSerializableData)
        {
            dataStorage.AddSubcomponent(item.GetData());
        }
        return dataStorage;
    }

    bool Selectable.IsDeadInside()
    {
        return false;
    }

    void Damageable.ApplyDamage(int damage)
    {
        HP = HP - damage;
    }

    public string GetFaction()
    {
        return faction;
    }

    Vector3 Targetable.GetShootPosition()
    {
        return transform.position;
    }

    bool Targetable.IsTargedDeadInside()
    {
        return false;
    }
    public void ResolveGraphics()
    {
        string newState = faction;
        if (HP > Mathf.FloorToInt(0.75f * MaxHP)) newState = faction;
        //Tu kolejne warunki stan�w grafiki

        if (newState!=graphicsState)
        {
            graphicsState = newState;
            ObjectiveSprites sprites = graphics.FindGraphicsState(newState);
            if (sprites != null)
            {
                animator.SetSpriteList(sprites.stateSprite, sprites.animSpeed);
            }
        }
    }

    (string, Targetable) AIControllable.CurrentState()
    {
        throw new System.NotImplementedException();
    }

    void AIControllable.ReciveOrder(string order, (int, int)? target)
    {
        switch (order)
        {
            case "target":
                (int, int) ntarget;
                try
                {
                    ntarget = ((int, int))target;
                }
                catch (System.Exception)
                {
                    Debug.LogError("No pathfind target");
                    return;
                }
                gatherSpot = ntarget;
                break;
        }
    }

    void Captureable.TryCapturing(int value,string faction)
    {
        TryIncrementingCaptureProgress(value, faction);
    }
    [System.Serializable]
    public class CaptureProgress
    {
        public int value = 0;
        public string faction = "";
        public CaptureProgress(int value,string faction)
        {
            this.value = value;
            this.faction = faction;
        }
    }
}