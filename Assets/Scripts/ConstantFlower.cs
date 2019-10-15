using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantFlower : MonoBehaviour, Food
{   
    [SerializeField]
    float size = 0f;
    [SerializeField]
    float growthRate = 0.5f;
    [SerializeField]
    float timeBetweenRespawns = 5f;
    [SerializeField]
    float nutritionalValue = 1f;

    LifeForm lifeForm;
    SpriteRenderer sr;

    void Start()
    {
        this.lifeForm = this.GetComponentInChildren<LifeForm>();
        this.sr = this.GetComponentInChildren<SpriteRenderer>();

        this.lifeForm.deathEvent += Die;

        ResetState();
    }

    void Update()
    {
        this.size = Mathf.Clamp01(this.size + this.growthRate * Time.deltaTime);
        this.transform.localScale = Vector2.one * this.size;
    }

    void ResetState()
    {
        this.sr.color = HushPuppy.getColorWithOpacity(this.sr.color, 1f);
        this.size = 0f;
        this.lifeForm.ResetState();
    }

    public void Die() { StartCoroutine(Die_Coroutine()); }
    public IEnumerator Die_Coroutine()
    {
        this.sr.color = HushPuppy.getColorWithOpacity(this.sr.color, 0.3f);

        yield return new WaitForSeconds(timeBetweenRespawns);

        ResetState();
    }

    public float GetNutritionalValue()
    {
        return this.size * this.nutritionalValue;
    }
}
