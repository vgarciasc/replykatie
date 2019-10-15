using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TriggerObservation
{
    public string tag;
    public List<GameObject> objects = new List<GameObject>();
}

public class TriggerObservationCompiler : MonoBehaviour, PoolableResettable
{
    public List<TriggerObservation> triggerObservations = new List<TriggerObservation>();
    
    float radius = -1f;

    Creature _creature;
    Transform _transform;
    Coroutine _customUpdateCoroutine;

    void Start() {
        ResetState();
    }

    public void ResetState() {
        _transform = this.transform;
        _creature = this.GetComponentInChildren<Creature>();

        radius = _creature.viewDistance;

        if (_customUpdateCoroutine != null) {
            StopCoroutine(_customUpdateCoroutine);
        }
        _customUpdateCoroutine = StartCoroutine(CustomUpdate());
    }

    IEnumerator CustomUpdate() {
        while (true) {
            var hits = Physics2D.CircleCastAll(
                _transform.position,
                this.radius,
                Vector2.zero,
                0f,
                LayerMask.GetMask("Plants", "Animals")
            );

            foreach (var triggerObservation in triggerObservations) {
                triggerObservation.objects = new List<GameObject>();
            }

            foreach (var hit in hits) {
                var obj = hit.transform.gameObject;
                
                int tagIndex = triggerObservations.FindIndex((f) => obj.CompareTag(f.tag));
                if (tagIndex == -1) continue; // not observing this tag
                int objIndex = triggerObservations[tagIndex].objects.FindIndex((f) => f == obj);
                if (objIndex != -1) continue; // already observed this object

                triggerObservations[tagIndex].objects.Add(obj);
            }

            yield return new WaitForSeconds(0.2f);
        }
    }

    // private void OnTriggerEnter2D(Collider2D collision)
    // {
    //     var obj = collision.gameObject;
    //     GameObjectEnter(obj);
    // }

    private void GameObjectEnter(GameObject obj)
    {
        int tagIndex = triggerObservations.FindIndex((f) => obj.CompareTag(f.tag));
        if (tagIndex == -1) return; // not observing this tag

        int objIndex = triggerObservations[tagIndex].objects.FindIndex((f) => f == obj);
        if (objIndex != -1) return; // already observed this object

        triggerObservations[tagIndex].objects.Add(obj);
    }

    // private void OnTriggerExit2D(Collider2D collision)
    // {
    //     var obj = collision.gameObject;

    //     int tagIndex = triggerObservations.FindIndex((f) => obj.CompareTag(f.tag));
    //     if (tagIndex == -1) return; // not observing

    //     int objIndex = triggerObservations[tagIndex].objects.FindIndex((f) => f == obj);
    //     if (objIndex == -1) {
    //         // Debug.LogError("This should not be happening.");
    //         // print(obj + " wasn't observed, but exited");
    //         // Debug.Break(); 
    //         return; // not observed this object
    //     }

    //     triggerObservations[tagIndex].objects.RemoveAt(objIndex);
    // }

    public List<GameObject> GetObservationsByTag(string tag)
    {
        return triggerObservations.Find((f) => f.tag == tag).objects;
    }
}
