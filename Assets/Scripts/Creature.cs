using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Creature : MonoBehaviour
{
    // Necessities
    [Header("Necessities")]

    [SerializeField]
    [Range(-1f, 1f)]
    float hunger = 0;
    [SerializeField]
    [Range(0f, 1f)]
    float thirst = 0;
    [SerializeField]
    [Range(0f, 1f)]
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
        //UpdateReproductiveUrge();

        HandleHunger();
        //HandleThirst();
        //HandleReproductiveUrge();

        HandleIdle();
    }

    void UpdateHunger() {
        this.hunger = Mathf.Min(1f, this.hunger + Time.deltaTime * this.hungerRate);
    }

    void HandleHunger() {
        var hungerProcess = new IntentProcess(
            this.hunger * 2f,
            IntentProcessKind.SEARCH_FOOD,
            new List<IntentProcessKind>() { IntentProcessKind.GO_FOOD }
        );

        cpu.Interrupt(hungerProcess);
    }

    void HandleIdle() {
        var idleProcess = new IntentProcess(
            1,
            IntentProcessKind.IDLE,
            null
        );

        cpu.Interrupt(idleProcess);
    }

    public IEnumerator GetProcessCoroutine(IntentProcess process) {
        switch (process.kind) {
            case IntentProcessKind.SEARCH_FOOD:
                while (true) { yield return RandomWalk(); }
                break;
            case IntentProcessKind.GO_FOOD:
                yield return WalkTo(process.position);
                break;
            case IntentProcessKind.EAT_FOOD:
                var food = process.target;
                if (IsObjectAvailable(food)) {
                    yield return Eat(food);
                    Destroy(food);
                }
                break;
            case IntentProcessKind.IDLE:
                while (true) {
                    yield return RandomWalk();
                    yield return new WaitForSeconds(1f);
                }
                break;
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
        this.hunger = this.hunger - 1f;

        yield return new WaitForSeconds(1f);

        this.transform.DOScale(1.1f, 0.3f).SetEase(Ease.InBounce);
    }
    #endregion

    #region Sensing
    void OnTriggerStay2D(Collider2D collision) {
        var obj = collision.gameObject;
        float distance = Vector3.Distance(this.transform.position, obj.transform.position);

        if (obj.tag == "Flower") {
            // close quarters
            if (distance < 0.6f) {
                var goFoodProcess = new IntentProcess(
                    5,
                    IntentProcessKind.EAT_FOOD,
                    null,
                    obj.transform.position,
                    obj
                );
                cpu.Interrupt(goFoodProcess);   
            }
            // so far away
            else {
                var goFoodProcess = new IntentProcess(
                    this.hunger * 3f,
                    IntentProcessKind.GO_FOOD,
                    new List<IntentProcessKind>() { IntentProcessKind.EAT_FOOD },
                    obj.transform.position,
                    obj
                );
                cpu.Interrupt(goFoodProcess);
            }
        }
    }
    #endregion

    bool IsObjectAvailable(GameObject obj) {
        return obj != null && obj.activeSelf;
    }
}
