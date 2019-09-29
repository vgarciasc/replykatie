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
    [SerializeField]
    float lifetime = 0f;
    [SerializeField]
    float timeOfDeath = 0f;

    [Header("Attributes")]

    [SerializeField]
    Vector2 timeBetweenSpreads = new Vector2(7f, 15f);
    [SerializeField]
    float spreadRadius = 3f;
    [SerializeField]
    Vector2 lifeExpectancyRange = new Vector2(20f, 25f);
    [SerializeField]
    float growingRate = 0.25f;

    [Header("Prefabs")]

    [SerializeField]
    string plantPrefabName;

    // Components
    SpriteRenderer sr;
    MapManager mapManager;
    
    void Start()
    {
        this.sr = this.GetComponentInChildren<SpriteRenderer>();
        this.mapManager = MapManager.GetMapManager();

        this.timeOfDeath = HushPuppy.RandomFloat(this.lifeExpectancyRange);

        StartCoroutine(Spread());
    }

    void Update()
    {
        HandleSize();
        HandleLife();
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

    void HandleLife() {
        this.lifetime += Time.deltaTime;
        if (this.lifetime > this.timeOfDeath) {
            StartCoroutine(Die());
        }      
    }

    IEnumerator Die() {
        this.sr.DOFade(0f, 1f);
        yield return new WaitForSeconds(1f);
        Destroy(this.gameObject);
    }
}
