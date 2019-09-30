using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Plant : MonoBehaviour
{
    [Header("State")]

    [SerializeField]
    [Range(0f, 1f)]
    float size = 1f;

    [Header("Attributes")]

    [SerializeField]
    Vector2 timeBetweenSpreads = new Vector2(7f, 15f);
    [SerializeField]
    float spreadRadius = 3f;
    [SerializeField]
    float growingRate = 0.25f;

    [Header("Prefabs")]

    [SerializeField]
    string plantPrefabName;

    // Components
    SpriteRenderer sr;
    MapManager mapManager;
    LifeForm lifeForm;
    
    void Start()
    {
        this.sr = this.GetComponent<SpriteRenderer>();
        this.lifeForm = this.GetComponent<LifeForm>();

        this.mapManager = MapManager.GetMapManager();

        this.lifeForm.deathEvent += Die;

        StartCoroutine(Spread());
    }

    void Update()
    {
        HandleSize();
    }

    #region Reproduction
    IEnumerator Spread() {
        yield return new WaitUntil(() => size > 0.8f);

        while (true) {
            float timeToSpread = HushPuppy.RandomFloat(timeBetweenSpreads);
            yield return new WaitForSeconds(timeToSpread);

            Generate();
        }
    }

    void Generate() {
        Vector3 position = HushPuppy.GenerateValidPosition(
            () => this.transform.position + (Vector3) (this.spreadRadius * Random.insideUnitCircle),
            (vec) => mapManager.IsPositionValid(vec));
        
        GameObject plantPrefab = (GameObject) Resources.Load("Prefabs/" + plantPrefabName);
        var obj = Instantiate(plantPrefab, position, Quaternion.identity);
        obj.transform.SetParent(this.transform.parent);

        var plant = obj.GetComponent<Plant>();
        plant.GetBorth();
    }

    public void GetBorth() {
        this.size = 0f;
        this.transform.localScale = Vector3.zero;
    }
    #endregion

    void HandleSize() {
        this.size = Mathf.Clamp01(this.size + Time.deltaTime * this.growingRate);
        this.transform.localScale = Vector3.one * size;
    }

    void Die() {StartCoroutine(Die_Coroutine());}
    IEnumerator Die_Coroutine() {
        this.sr.DOFade(0f, 1f);
        yield return new WaitForSeconds(1f);
        Destroy(this.gameObject);
    }

    public float GetNutritionalValue() {
        return size;
    }
}
