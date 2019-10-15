using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeForm : MonoBehaviour, PoolableResettable
{
    public delegate void voidDelegate();
    public event voidDelegate deathEvent;

    [SerializeField]
    public float lifetime = 0f;
    [SerializeField]
    float timeOfDeath = 0f;
    [SerializeField]
    Vector2 lifeExpectancyRange = new Vector2(20f, 25f);
    [SerializeField]
    bool isDead = false;

    void Start()
    {
        ResetState();
    }

    public void ResetState() {
        this.timeOfDeath = HushPuppy.RandomFloat(this.lifeExpectancyRange);
        this.lifetime = 0f;
        this.isDead = false;
    }

    void Update()
    {
        HandleLife();
    }

    void HandleLife() {
        if (IsDead()) return;

        this.lifetime += Time.deltaTime;
        if (this.lifetime > this.timeOfDeath) {
            Death();
        }      
    }

    public void Death() {
        isDead = true;

        if (deathEvent != null) {
            deathEvent();
        }

        //var plant = this.GetComponentInChildren<ConstantFood>();
        //if (plant != null) {
        //    plant.Die();
        //}
    }

    public bool IsDead() {
        return isDead;
    }
}
