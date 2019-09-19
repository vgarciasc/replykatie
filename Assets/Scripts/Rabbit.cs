using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rabbit : MonoBehaviour
{
    enum Gender { UNDEFINED, FEMALE, MALE };
    enum State { IDLE, IDLE_WALK, FOOD };
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
        public static Intent FoodIntent(Vector3 position, int priority = 1) {
            return new Intent(State.FOOD, priority, position);
        }
    }

    // Creature State
    [SerializeField]
    State currentState = State.IDLE;

    List<Intent> intents = new List<Intent>();

    [SerializeField]
    [Range(0f, 1f)]
    float hunger = 0f;

    // Attributes
    [SerializeField]
    float speed = 2f;
    [SerializeField]
    Gender gender = Gender.UNDEFINED;

    Rigidbody2D rb;

    Coroutine currentManageIntent = null;
    Intent currentIntent = null;

    void Start()
    {
        rb = this.GetComponentInChildren<Rigidbody2D>();

        currentManageIntent = StartCoroutine(ManageIntent(Intent.IdleIntent()));
    }

    void Update()
    {
        this.hunger = Mathf.Clamp01(this.hunger + Time.deltaTime * 0.1f);
    }

    void RegisterNewIntent(Intent intent)
    {
        this.intents.Add(intent);
        this.intents.Sort((x, y) => x.priority.CompareTo(y.priority));
        Intent hi_priority_intent = this.intents[0];

        if (hi_priority_intent != currentIntent)
        {
            if (currentManageIntent != null) StopCoroutine(currentManageIntent);
            currentManageIntent = StartCoroutine(ManageIntent(hi_priority_intent));
        }
    }

    IEnumerator ManageIntent(Intent intent)
    {
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
            case State.FOOD:
                yield return WalkTo(intent.position);
                var food = GetCloseObjectWithTag("Flower");
                if (food != null)
                {
                    Destroy(food);
                    Eat(food);
                }
                break;
        }

        currentManageIntent = StartCoroutine(ManageIntent(nextIntent));
    }

    #region movement
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
    #endregion

    private void OnTriggerStay2D(Collider2D collision)
    {
        var obj = collision.gameObject;
        if (obj.tag == "Flower" && this.hunger > 0.5f)
        {
            var target = this.transform.position + (obj.transform.position - this.transform.position).normalized * 0.8f;
            RegisterNewIntent(Intent.FoodIntent(target));
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

    IEnumerator Eat(GameObject food)
    {
        this.hunger = Mathf.Clamp01(this.hunger - 0.5f);

        yield return new WaitForSeconds(1f);

        this.transform.localScale *= 2f;
    }
}
