using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeForm : MonoBehaviour
{
    public delegate void voidDelegate();
    public event voidDelegate deathEvent;

    [SerializeField]
    public float lifetime = 0f;
    [SerializeField]
    float timeOfDeath = 0f;
    [SerializeField]
    Vector2 lifeExpectancyRange = new Vector2(20f, 25f);

    bool isDead = false;

    void Start()
    {
        this.timeOfDeath = HushPuppy.RandomFloat(this.lifeExpectancyRange);
    }

    void Update()
    {
        HandleLife();
    }

    void HandleLife() {
        if (this.isDead) return;

        this.lifetime += Time.deltaTime;
        if (this.lifetime > this.timeOfDeath) {
            if (deathEvent != null) {
                deathEvent();
            }
            
            isDead = true;
        }      
    }
}
