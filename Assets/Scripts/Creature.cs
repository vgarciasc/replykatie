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
    [SerializeField]
    float GESTATION_DURATION = 10f;
    [SerializeField]
    float GESTATION_SLOWDOWN = 2f;
    [SerializeField]
    Vector2 OFFSPRING = new Vector2(1, 3);

    [Header("State")]

    [SerializeField]
    [Range(-1f, 1f)]
    float hunger = 0;
    [SerializeField]
    [Range(0f, 1f)]
    float reproductiveUrge = 0;
    // [SerializeField]
    float reproductiveUrge_x = Mathf.PI;
    [SerializeField]
    float health = 0;
    [SerializeField]
    [Range(0f, 1f)]
    float size = 0;
    [SerializeField]
    float growthRate = 0.25f;
    [SerializeField]
    bool gestating = false;

    [Header("Default Values")]
    [SerializeField]
    float default_hunger = 0;
    [SerializeField]
    float default_reproductiveUrge = 0;
    [SerializeField]
    float default_reproductiveUrge_x = Mathf.PI;
    [SerializeField]
    float default_health = Mathf.PI;
    [SerializeField]
    float default_size = 0;
    [SerializeField]
    bool default_gestating = false;

    [Header("Attributes")]

    [SerializeField]
    Gender gender = Gender.UNDEFINED;

    [Header("Genes")]
    public float viewDistance = 2f;
    public float baseSpeed = 2f;

    [Header("Prefabs")]

    [SerializeField]
    string creaturePrefabName;

    // Components
    CPU cpu;
    Rigidbody2D rb;
    SpriteRenderer sr;
    MapManager mapManager;
    ObjectPoolManager opm;
    LifeForm lifeForm;
    TriggerObservationCompiler toc;

    Coroutine deluxeUpdate;

    void Start()
    {
        this.cpu = this.GetComponent<CPU>();
        this.rb = this.GetComponent<Rigidbody2D>();
        this.sr = this.GetComponent<SpriteRenderer>();
        this.lifeForm = this.GetComponent<LifeForm>();
        this.toc = this.GetComponent<TriggerObservationCompiler>();
        
        this.mapManager = MapManager.GetMapManager();
        this.opm = ObjectPoolManager.GetObjectPoolManager();
        
        this.lifeForm.deathEvent += Die;
        
        ResetState();
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

    public void ResetState() {
        this.hunger = this.default_hunger;
        this.reproductiveUrge = this.default_reproductiveUrge;
        this.reproductiveUrge_x = this.default_reproductiveUrge_x;
        this.health = this.default_health;
        this.size = this.default_size;
        this.transform.localScale = Vector3.one;
        this.gestating = this.default_gestating;

        if (deluxeUpdate != null) {
            StopCoroutine(deluxeUpdate);
        }
        deluxeUpdate = StartCoroutine(DeluxeUpdate());
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
        if (this.gestating || this.lifeForm.lifetime < 10f) {
            this.reproductiveUrge_x = this.gender == Gender.FEMALE ? Mathf.PI : 0f;
            this.reproductiveUrge = 0f;
            return;
        }
        
        if (this.gender == Gender.FEMALE)
        {
            // Reproductive urge of female creatures behaves like a cosinusoidal function
            this.reproductiveUrge_x = (this.reproductiveUrge_x + Time.deltaTime * this.reproductiveUrgeRate) % (2f * Mathf.PI);
            this.reproductiveUrge = 0.5f * (1f + Mathf.Cos(this.reproductiveUrge_x));   
        }
        else if (this.gender == Gender.MALE)
        {
            // Reproductive urge of male creatures behaves like a linear positive function
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
        CleanContext();

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
                if (IsFoodAvailable(food)) {
                    yield return Eat(food);
                }
                break;
            case IntentProcessKind.COPULATE:
                var mate = process.target.GetComponent<Creature>();
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
        Vector3 destination = HushPuppy.GenerateValidPosition(
            () => this.transform.position + 3f * ((Vector3) Random.insideUnitCircle),
            (vec) => mapManager.IsPositionValid(vec)
        );

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
        float nutritionalValue = food.GetComponent<Plant>().GetNutritionalValue();
        food.GetComponentInChildren<LifeForm>().Death();

        yield return new WaitForSeconds(0.2f);

        if (food == null) {
            yield break;
        }

        float animDuration = 0.1f;
        this.transform.DOScale(
            this.transform.localScale * 1.05f,
            animDuration).SetEase(Ease.InBounce);
        yield return new WaitForSeconds(animDuration);
        
        this.hunger = this.hunger - 1f * nutritionalValue;
        
        yield return new WaitForSeconds(0.2f);
    }

    IEnumerator Copulate(Creature mate)
    {
        if (this.gender == Gender.MALE)
        {
            var mateProcess = IntentProcess.Proc_Copulate(15f, this.gameObject);
            mate.GetComponent<CPU>().Interrupt(mateProcess);
        }
        else {
            StartCoroutine(Gestate(this, mate));
        }
        yield return new WaitForSeconds(1f);
        this.reproductiveUrge_x = this.gender == Gender.FEMALE ? Mathf.PI : 0f;
        this.reproductiveUrge = 0f;
    }
    #endregion

    #region Reproduction
    IEnumerator Gestate(Creature mother, Creature father) {
        this.gestating = true;
        this.baseSpeed /= GESTATION_SLOWDOWN;
        
        yield return new WaitForSeconds(GESTATION_DURATION);

        int offspring = HushPuppy.RandomInt(this.OFFSPRING);
        for (int i = 0; i < offspring; i++) {
            GiveBirth(mother, father);
        }

        this.gestating = false;
        this.baseSpeed *= GESTATION_SLOWDOWN;
    }

    void GiveBirth(Creature mother, Creature father) {
        if (this.health < 0.75f) return;

        //var obj = Instantiate(
        //    (GameObject) Resources.Load("Prefabs/" + this.creaturePrefabName),
        //    this.transform.position,
        //    Quaternion.identity);
        //obj.transform.SetParent(this.transform.parent);

        var obj = opm.Spawn(PoolableObjectKinds.RABBIT);
        obj.transform.position = this.transform.position;

        //print(obj.name);
        //Debug.Break();

        var offspring = obj.GetComponentInChildren<Creature>();
        offspring.GetBorth(mother, father);
    }

    public void GetBorth(Creature mother, Creature father) {
        Start();
        PassGenes(mother, father);

        cpu.ResetState();
        this.gender = Random.Range(0, 10) % 2 == 0 ? Gender.FEMALE : Gender.MALE;
        this.health = mother.health;
        this.sr.color = this.gender == Gender.FEMALE ?
            (new Color(0.8f, 0.1f, 0.2f)) + Color.white * Random.Range(-0.2f, 0.2f) :
            (new Color(0.2f, 0.3f, 0.8f)) + Color.white * Random.Range(-0.2f, 0.2f);
    }
    #endregion

    #region Genetics
    void PassGenes(Creature mother, Creature father) {
        this.viewDistance = Genetics.CombineGenes(
            mother.viewDistance, father.viewDistance);
        this.baseSpeed = Genetics.CombineGenes(
            mother.baseSpeed, father.baseSpeed);
    }
    #endregion

    #region Sensing

    IEnumerator DeluxeUpdate()
    {
        while (true)
        {
            foreach (var food in toc.GetObservationsByTag("Food")) {
                HandleFood(food);
            }

            foreach (var creature in toc.GetObservationsByTag("Creature"))
            {
                HandleCreature(creature);
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    void HandleFood(GameObject obj) {
        if (obj == null) return;

        float distance = Vector3.Distance(this.transform.position, obj.transform.position);
        var food = obj.GetComponentInChildren<Plant>();

        var foodProcess = distance < TOUCHING_DISTANCE ?
            IntentProcess.Proc_EatFood(this.hunger * 5f, obj)
            : IntentProcess.Proc_GoFood(this.hunger * 3f, obj);
        cpu.Interrupt(foodProcess);
    }

    void HandleCreature(GameObject obj)
    {
        if (obj == null) return;

        float distance = Vector3.Distance(this.transform.position, obj.transform.position);
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
    #endregion

    #region Health
    void HandleHealth()
    {
        this.health = 1 - (this.hunger * 0.6f);
        if (health <= 0.5f)
        {
            Die();
        }
    }

    void Die() {StartCoroutine(Die_Coroutine());}
    IEnumerator Die_Coroutine() {
        this.sr.DOFade(0f, 1f);
        yield return new WaitForSeconds(1f);
        //Destroy(this.gameObject);
        opm.Despawn(this.gameObject);
    }
    #endregion

    bool IsObjectAvailable(GameObject obj) {
        return obj != null && obj.activeSelf;
    }

    bool IsFoodAvailable(GameObject obj) {
        return IsObjectAvailable(obj) && !obj.GetComponentInChildren<LifeForm>().IsDead();
    }
}
