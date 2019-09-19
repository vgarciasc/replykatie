using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rabbit : MonoBehaviour
{
    enum Gender { UNDEFINED, FEMALE, MALE };
    enum State { IDLE, IDLE_WALK, GO_FOOD, SEARCH_FOOD };
    
    [System.Serializable]
    class Intent
    {
        public State state;
        public Vector3 position;
        public int priority;

        public Intent(State state, int priority, Vector3 position)
        {
            this.state = state;
            this.priority = priority;
            this.position = position;
        }

        public static Intent IdleIntent() { return new Intent(State.IDLE, 0, Vector3.zero); }
        public static Intent IdleWalkIntent() { return new Intent(State.IDLE_WALK, 0, Vector3.zero); }
        public static Intent SearchFoodIntent() { return new Intent(State.SEARCH_FOOD, 5, Vector3.zero); }
        public static Intent FoodIntent(Vector3 position, int priority = 1) {
            return new Intent(State.GO_FOOD, priority, position);
        }
    }

    // Creature State
    [SerializeField]
    List<Intent> intents = new List<Intent>();

    [SerializeField]
    [Range(0f, 1f)]
    float hunger = 0f;

    // Attributes
    [SerializeField]
    float speed = 2f;
    [SerializeField]
    float hungerRate = 1f;
    [SerializeField]
    Gender gender = Gender.UNDEFINED;

    // Constants
    float HUNGER_THRESHOLD_INITIAL = 0.4f;
    float HUNGER_THRESHOLD_CRITICAL = 0.7f;

    // Components
    Rigidbody2D rb;

    Coroutine currentManageIntent = null;
    [SerializeField]
    Intent currentIntent = null;

    void Start()
    {
        rb = this.GetComponentInChildren<Rigidbody2D>();

        currentManageIntent = StartCoroutine(ManageIntent(Intent.IdleIntent()));
    }

    void Update()
    {
        UpdateHunger();
        HandleHunger();
    }
    
    #region Intents
    void SortIntents() { this.intents.Sort((x, y) => y.priority.CompareTo(x.priority)); }
    Intent GetIntentByState(State state) { return intents.Find((f) => f.state == state); }
    Intent GetHighestPriorityIntent() { SortIntents(); return this.intents[0]; }

    void RegisterNewIntent(Intent intent, bool newOnly = false, bool updateIfExists = false)
    {
        if (updateIfExists) {
            var existing_intent = GetIntentByState(intent.state);
            if (existing_intent != null) {
                this.intents.Remove(existing_intent);
            }
        }

        if (newOnly) {
            if (GetIntentByState(intent.state) != null) return;
        }

        this.intents.Add(intent);
        Intent hi_priority_intent = GetHighestPriorityIntent();
        print(">> '" + this.gameObject.name + "': adding intent [" + System.Enum.GetName(typeof(State), intent.state) + "], hi-priority is [" + System.Enum.GetName(typeof(State), hi_priority_intent.state) + "]");

        if (hi_priority_intent != currentIntent)
        {
            print(">> '" + this.gameObject.name + "': high-priority changed from [" + System.Enum.GetName(typeof(State), currentIntent.state) + "] to [" + System.Enum.GetName(typeof(State), hi_priority_intent.state) + "]");

            if (currentManageIntent != null) StopCoroutine(currentManageIntent);
            currentManageIntent = StartCoroutine(ManageIntent(hi_priority_intent));
        }
    }

    IEnumerator ManageIntent(Intent intent)
    {
        currentIntent = intent;
        Intent nextIntent = Intent.IdleIntent();

        switch (intent.state)
        {
            case State.IDLE:
                yield return new WaitForSeconds(Random.Range(2f, 3f));

                int prob = 11;
                int dice = Random.Range(0, 10);

                nextIntent = (dice < prob) ? Intent.IdleWalkIntent() : Intent.IdleIntent();
                break;
            case State.IDLE_WALK:
                yield return RandomWalk();
                break;
            case State.GO_FOOD:
                print("asdf 1");
                yield return WalkTo(intent.position);
                print("asdf 2");
                var food = GetCloseObjectWithTag("Flower");
                print(food);
                print("asdf 3");
                if (food != null)
                {
                    print("asdf 4");
                    Destroy(food);
                    Eat(food);
                }
                break;
            case State.SEARCH_FOOD:
                while (this.hunger > HUNGER_THRESHOLD_CRITICAL) {
                    yield return RandomWalk();
                }
                break;
        }

        currentIntent = null;
        currentManageIntent = StartCoroutine(ManageIntent(nextIntent));
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

        this.rb.velocity = (destination - this.transform.position).normalized * speed;
        yield return new WaitUntil(() => 
            Vector3.Distance(this.transform.position, destination) < distance_error 
            || ((Time.time - startedMovingTimestamp) > max_time_walking)
        );
        this.rb.velocity = Vector3.zero;
    }

    IEnumerator Eat(GameObject food)
    {
        this.hunger = Mathf.Clamp01(this.hunger - 0.5f);

        yield return new WaitForSeconds(1f);

        this.transform.localScale *= 2f;
    }
    #endregion

    #region Sensoring
    private void OnTriggerStay2D(Collider2D collision)
    {
        var obj = collision.gameObject;
        if (obj.tag == "Flower" && this.hunger > HUNGER_THRESHOLD_INITIAL)
        {
            var target = this.transform.position + (obj.transform.position - this.transform.position).normalized * 0.8f;
            
            if (this.hunger > HUNGER_THRESHOLD_CRITICAL) {
                if (GetHighestPriorityIntent().state != State.GO_FOOD) {
                    RegisterNewIntent(Intent.FoodIntent(target, priority: 10), updateIfExists: true);
                }
            }
            else {
                RegisterNewIntent(Intent.FoodIntent(target, priority: 1), newOnly: true);
            }
        }
    }
    RaycastHit2D[] GetVicinity() { return Physics2D.CircleCastAll(this.transform.position, 0.2f, Vector2.zero); }

    GameObject GetCloseObjectWithTag(string tag)
    {
        foreach (var hit in GetVicinity())
        {
            var obj = hit.transform.gameObject;
            if (obj.tag == tag) {
                return obj;        
            }
        }

        return null;
    }
    #endregion

    #region Hunger
    void UpdateHunger()
    {
        this.hunger = Mathf.Clamp01(this.hunger + Time.deltaTime * 0.1f) * hungerRate;
    }

    void HandleHunger()
    {
        if (this.hunger > HUNGER_THRESHOLD_CRITICAL)
        {
            // var go_food_intent = GetIntentByState(State.GO_FOOD);

            RegisterNewIntent(Intent.SearchFoodIntent(), newOnly: true);
        }
    }
    #endregion
}
