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
    int neighbors = 0;
    [SerializeField]
    float actualGrowthRate = 0f;

    [Header("Attributes")]

    [SerializeField]
    Vector2 timeBetweenSpreads = new Vector2(7f, 15f);
    [SerializeField]
    float timeBetweenRespawns = 3f;
    [SerializeField]
    int maxSpreadOffspring = 4;
    [SerializeField]
    float spreadRadius = 3f;
    [SerializeField]
    float growthRate = 0.25f;
    [SerializeField]
    float growthPenaltyForNeighbor = 0.5f;
    [SerializeField]
    float neighborRadius = 1f;
    [SerializeField]
    float nutritionalValue = 2f;
    [SerializeField]
    bool shouldRespawn = false;

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

            int offspring = Random.Range(1, this.maxSpreadOffspring + 1);
            for (int i = 0; i < offspring; i++) {
                Generate();
            }
        }
    }

    void Generate() {
        Vector3 position = HushPuppy.GenerateValidPosition(
            () => this.transform.position + (Vector3) (this.spreadRadius * Random.insideUnitCircle),
            (vec) => mapManager.IsPositionValid(vec));
        
        Generate(position);
    }

    void Generate(Vector3 position) {
        GameObject plantPrefab = (GameObject) Resources.Load("Prefabs/" + plantPrefabName);
        var obj = Instantiate(plantPrefab, position, Quaternion.identity);
        obj.transform.SetParent(this.transform.parent);

        var plant = obj.GetComponent<Plant>();
        plant.GetBorth();
    }

    public void GetBorth() {
        this.size = 0f;
        this.transform.localScale = Vector3.zero;

        float growthPenalty = 1f;

        if (!shouldRespawn) {
            var possibleNeighbors = Physics2D.CircleCastAll(
                this.transform.position,
                this.neighborRadius,
                Vector2.zero);
            foreach (var hit in possibleNeighbors) {
                if (hit.transform.GetComponentInChildren<Plant>() != null) {
                    this.neighbors++;
                }
            }

            if (this.neighbors > 3) {
                Die();
            }

            growthPenalty = Mathf.Pow(this.growthPenaltyForNeighbor, this.neighbors);
        }

        this.actualGrowthRate = growthPenalty * this.growthRate;
    }
    #endregion

    void HandleSize() {this.size = Mathf.Clamp01(this.size + Time.deltaTime * actualGrowthRate);
        this.transform.localScale = Vector3.one * size;
    }

    bool hasRespawned = false;

    void Die() {StartCoroutine(Die_Coroutine());}
    IEnumerator Die_Coroutine() {
        this.sr.DOFade(0f, 1f);
        yield return new WaitForSeconds(1f);

        if (shouldRespawn && !hasRespawned) {
            yield return new WaitForSeconds(timeBetweenRespawns);
            this.Generate(this.transform.position);
            hasRespawned = true;
            yield return new WaitForSeconds(0.2f);
        }
        else {
            print("asdf");
        }

        Destroy(this.gameObject);
    }

    public float GetNutritionalValue() {
        return this.size * this.nutritionalValue;
    }
}
