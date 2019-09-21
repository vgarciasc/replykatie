using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public enum Gender { UNDEFINED, FEMALE, MALE };

public class Creature : MonoBehaviour
{
    // Necessities
    [Header("Necessities")]

    [SerializeField]
    [Range(-1f, 1f)]
    float hunger = 0;
    [SerializeField]
    [Range(-1f, 1f)]
    float thirst = 0;
    [SerializeField]
    [Range(-1f, 1f)]
    float reproductiveUrge = 0;

    [SerializeField]
    float hungerRate = 0.5f;
    [SerializeField]
    float thirstRate = 0.5f;
    [SerializeField]
    float reproductiveUrgeRate = 0.5f;
    
    [Header("Attributes")]
    [SerializeField]
    float baseSpeed = 2f;
    [SerializeField]
    Gender gender = Gender.UNDEFINED;

    Rigidbody2D rb;
    CPU cpu;

    [Header("Constants")]
    [SerializeField]
    float TOUCHING_DISTANCE = 0.7f;

    void Start()
    {
        this.cpu = this.GetComponent<CPU>();
        this.rb = this.GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        UpdateHunger();
        //UpdateThirst();
        // UpdateReproductiveUrge();

        HandleHunger();
        //HandleThirst();
        // HandleReproductiveUrge();

        HandleIdle();
    }

    #region Necessities
    void UpdateHunger() {
        this.hunger = Mathf.Min(1f, this.hunger + Time.deltaTime * this.hungerRate);
    }

    void HandleHunger() {
        var hungerProcess = IntentProcess.Proc_SearchFood(this.hunger * 2f);
        cpu.Interrupt(hungerProcess);
    }

    // void UpdateReproductiveUrge() {
    //     this.reproductiveUrge = Mathf.Min(1f, this.reproductiveUrge + Time.deltaTime * this.reproductiveUrgeRate);
    // }

    // void HandleReproductiveUrge() {
    //     var matingProcess = IntentProcess.Proc_SearchMate(this.reproductiveUrge * 2f);
    //     cpu.Interrupt(matingProcess);
    // }

    void HandleIdle() {
        var idleProcess = IntentProcess.Proc_Idle();
        cpu.Interrupt(idleProcess);
    }
    #endregion

    #region Process
    public IEnumerator GetProcessCoroutine(IntentProcess process) {
        switch (process.kind) {
            case IntentProcessKind.SEARCH_FOOD:
                while (true) { yield return RandomWalk(); }
            case IntentProcessKind.GO_FOOD:
                yield return WalkTo(process.position);
                break;
            case IntentProcessKind.EAT_FOOD:
                var food = process.target;
                if (IsObjectAvailable(food)) {
                    yield return Eat(food);
                }
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
    #endregion

    #region Sensing
    void OnTriggerStay2D(Collider2D collision) {
        var obj = collision.gameObject;
        float distance = Vector3.Distance(this.transform.position, obj.transform.position);

        if (obj.tag == "Food") {
            var goFoodProcess = distance < TOUCHING_DISTANCE ? 
                IntentProcess.Proc_EatFood(5f, obj)
                : IntentProcess.Proc_GoFood(this.hunger * 3f, obj);
            cpu.Interrupt(goFoodProcess);
        }
    }
    #endregion

    bool IsObjectAvailable(GameObject obj) {
        return obj != null && obj.activeSelf;
    }
}
