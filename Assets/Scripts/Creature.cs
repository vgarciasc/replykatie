using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum Gender { UNDEFINED, FEMALE, MALE };

public class Creature : MonoBehaviour
{
    [Header("Constants")]

    [SerializeField]
    float hungerRate = 0.5f;
    [SerializeField]
    float thirstRate = 0.5f;
    [SerializeField]
    float reproductiveUrgeRate = 0.5f;
    [SerializeField]
    float TOUCHING_DISTANCE = 0.7f;

    [Header("Necessities")]

    [SerializeField]
    [Range(-1f, 1f)]
    float hunger = 0;
    [SerializeField]
    [Range(-1f, 1f)]
    float thirst = 0;
    [SerializeField]
    [Range(0f, 1f)]
    float reproductiveUrge = 0;
    [SerializeField]
    float reproductiveUrge_x = Mathf.PI;
    [SerializeField]
    float health = 0;

    [Header("Attributes")]

    [SerializeField]
    float baseSpeed = 2f;
    [SerializeField]
    Gender gender = Gender.UNDEFINED;

    // Components
    Rigidbody2D rb;
    CPU cpu;

    void Start()
    {
        this.cpu = this.GetComponent<CPU>();
        this.rb = this.GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        UpdateHunger();
        //UpdateThirst();
        UpdateReproductiveUrge();

        HandleHunger();
        //HandleThirst();
        //HandleReproductiveUrge();

        HandleIdle();

        HandleHealth();
    }

    #region Necessities
    void UpdateHunger() {
        this.hunger = Mathf.Min(1f, this.hunger + Time.deltaTime * this.hungerRate);
    }

    void HandleHunger() {
        var hungerProcess = IntentProcess.Proc_SearchFood(this.hunger * 2f);
        cpu.Interrupt(hungerProcess);
    }

    void UpdateReproductiveUrge() {
        if (this.gender == Gender.FEMALE)
        {
            this.reproductiveUrge_x = (this.reproductiveUrge_x + Time.deltaTime * this.reproductiveUrgeRate) % (2f * Mathf.PI);
            this.reproductiveUrge = 0.5f * (1f + Mathf.Cos(this.reproductiveUrge_x));
        }
        else
        {
            this.reproductiveUrge = Mathf.Clamp01(this.reproductiveUrge + Time.deltaTime * this.reproductiveUrgeRate);
        }
    }

    void HandleIdle() {
        var idleProcess = IntentProcess.Proc_Idle();
        cpu.Interrupt(idleProcess);
    }
    #endregion

    #region Process
    public IEnumerator GetProcessCoroutine(IntentProcess process) {
        switch (process.kind) {
            case IntentProcessKind.SEARCH_FOOD: 
            case IntentProcessKind.SEARCH_MATE:
                while (true) { yield return RandomWalk(); }
            case IntentProcessKind.GO_FOOD:
            case IntentProcessKind.GO_MATE:
                yield return WalkTo(process.position);
                break;
            case IntentProcessKind.EAT_FOOD:
                var food = process.target;
                if (IsObjectAvailable(food)) {
                    yield return Eat(food);
                }
                break;
            case IntentProcessKind.COPULATE:
                var mate = process.target;
                yield return Copulate(mate);
                break;
            case IntentProcessKind.IDLE:
                while (true) {
                    yield return RandomWalk();
                    yield return new WaitForSeconds(1f);
                }
            default:
                Debug.LogError("This should not be happening. Process Kind: "
                    + System.Enum.GetName(typeof(IntentProcessKind), process.kind));
                Debug.Break();
                break;
        }

        cpu.EndProcess();
    }

    public void CleanContext() {
        this.rb.velocity = Vector3.zero;
    }
    #endregion
    
    #region Actions
    IEnumerator RandomWalk()
    {
        Vector3 direction = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        Vector3 destination = this.transform.position + direction;

        yield return WalkTo(destination);
    }

    IEnumerator WalkTo(Vector3 destination)
    {
        float distance_error = 0.2f;
        float max_time_walking = 3f;
        float startedMovingTimestamp = Time.time;

        this.rb.velocity = (destination - this.transform.position).normalized * baseSpeed;
        yield return new WaitUntil(() => 
            Vector3.Distance(this.transform.position, destination) < distance_error 
            || ((Time.time - startedMovingTimestamp) > max_time_walking)
        );
        this.rb.velocity = Vector3.zero;
    }

    IEnumerator Eat(GameObject food)
    {
        yield return new WaitForSeconds(1f);
        Destroy(food);

        float animDuration = 0.3f;
        this.transform.DOScale(1.1f, animDuration).SetEase(Ease.InBounce);
        yield return new WaitForSeconds(animDuration);
        
        this.hunger = this.hunger - 1f;
        
        yield return new WaitForSeconds(1f);
    }

    IEnumerator Copulate(GameObject mate)
    {
        if (this.gender == Gender.MALE)
        {
            var mateProcess = IntentProcess.Proc_Copulate(15f, this.gameObject);
            mate.GetComponent<CPU>().Interrupt(mateProcess);
        }
        yield return new WaitForSeconds(1f);
        this.reproductiveUrge_x = this.gender == Gender.FEMALE ? Mathf.PI : 0f;
        this.reproductiveUrge = 0f;
    }
    #endregion

    #region Sensing
    void OnTriggerStay2D(Collider2D collision) {
        var obj = collision.gameObject;
        float distance = Vector3.Distance(this.transform.position, obj.transform.position);

        if (obj.tag == "Food") {
            var foodProcess = distance < TOUCHING_DISTANCE ? 
                IntentProcess.Proc_EatFood(5f, obj)
                : IntentProcess.Proc_GoFood(this.hunger * 3f, obj);
            cpu.Interrupt(foodProcess);
        }
        else if (obj.tag == "Creature")
        {
            var targetCreature = obj.GetComponentInChildren<Creature>();
            if (this.gender == Gender.MALE && targetCreature.gender == Gender.FEMALE)
            {
                if (this.reproductiveUrge * targetCreature.reproductiveUrge > 0.7f)
                {
                    var mateProcess = distance < TOUCHING_DISTANCE ?
                        IntentProcess.Proc_Copulate(5f, obj)
                        : IntentProcess.Proc_GoMate(this.reproductiveUrge * targetCreature.reproductiveUrge * 2f, obj);
                    cpu.Interrupt(mateProcess);
                }
            }
        }
    }
    #endregion

    #region Health
    void HandleHealth()
    {
        this.health = 1 - (this.hunger * 0.5f + this.thirst * 0.5f);
        if (health <= 0.5f)
        {
            Die();
        }
    }

    void Die()
    {
        Destroy(this.gameObject);
    }
    #endregion

    bool IsObjectAvailable(GameObject obj) {
        return obj != null && obj.activeSelf;
    }
}
