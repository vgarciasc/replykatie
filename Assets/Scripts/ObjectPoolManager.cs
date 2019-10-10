using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PoolableObjectKinds { RABBIT };

[System.Serializable]
public class PoolableObject
{
    public GameObject prefab;
    public PoolableObjectKinds kind;
    public int amount;
    public Transform container;
    public bool autoscale;
    public List<GameObject> objects;
}

public class ObjectPoolManager : MonoBehaviour
{
    public static ObjectPoolManager GetObjectPoolManager() {
        return (ObjectPoolManager) HushPuppy.safeFindComponent("GameController", "ObjectPoolManager");
    }

    [SerializeField]
    List<PoolableObject> poolableObjects;

    void Start()
    {
        foreach (var poolableObject in this.poolableObjects) {
            InstantiateToPool(poolableObject, poolableObject.amount);
        }
    }

    public PoolableObject GetPoolableObjectByKind(PoolableObjectKinds kind)
    {
        return this.poolableObjects.Find((f) => f.kind == kind);
    }

    public GameObject Spawn(PoolableObjectKinds kind, bool shouldActivate = true)
    {
        var poolableObject = this.GetPoolableObjectByKind(kind);
        var objects = poolableObject.objects;
        for (int i = 0; i < objects.Count; i++) {
            if (!objects[i].activeInHierarchy) {
                if (shouldActivate) objects[i].SetActive(true);
                return objects[i];
            }
        }

        if (poolableObject.autoscale) {
            InstantiateToPool(poolableObject, objects.Count);
        }

        return Spawn(kind, shouldActivate);
    }

    public void Despawn(GameObject obj)
    {
        obj.SetActive(false);
    }

    public void CleanObservations()
    {
        foreach (var poolableObject in this.poolableObjects)
        {
            poolableObject.objects = poolableObject.objects.FindAll((f) => f.activeSelf);
        }
    }

    void InstantiateToPool(PoolableObject poolableObject, int amount)
    {
        string defaultName = System.Enum.GetName(typeof(PoolableObjectKinds), poolableObject.kind) + " #";
        var list = poolableObject.objects;
        if (poolableObject.objects == null)
        {
            poolableObject.objects = new List<GameObject>();
        }

        for (int i = 0; i < amount; i++)
        {
            GameObject obj = Instantiate(poolableObject.prefab, poolableObject.container);
            obj.SetActive(false);
            list.Add(obj);
            obj.name = defaultName + (list.Count).ToString();
        }
        poolableObject.objects = list;
    }

    //public List<BulletDeluxe> getAllBullets()
    //{
    //    List<BulletDeluxe> bulletScripts = new List<BulletDeluxe>();
    //    for (int i = 0; i < bullets.Count; i++)
    //    {
    //        bulletScripts.Add(bullets[i].GetComponent<BulletDeluxe>());
    //    }

    //    return bulletScripts;
    //}

    //public void destroyAllBullets()
    //{
    //    GameObject[] aux = GameObject.FindGameObjectsWithTag("Bullet");
    //    for (int i = 0; i < aux.Length; i++)
    //    {
    //        if (aux[i].GetComponent<BulletDeluxe>() != null)
    //        {
    //            aux[i].GetComponent<BulletDeluxe>().destroy();
    //        }
    //    }
    //}
}
