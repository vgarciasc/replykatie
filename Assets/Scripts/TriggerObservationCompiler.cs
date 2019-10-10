using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TriggerObservation
{
    public string tag;
    public List<GameObject> objects = new List<GameObject>();
}

public class TriggerObservationCompiler : MonoBehaviour
{
    public List<TriggerObservation> triggerObservations = new List<TriggerObservation>();

    private void OnTriggerEnter2D(Collider2D collision)
    {
        var obj = collision.gameObject;
        int tagIndex = triggerObservations.FindIndex((f) => obj.CompareTag(f.tag));

        if (tagIndex == -1) return; // not observing

        triggerObservations[tagIndex].objects.Add(obj);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var obj = collision.gameObject;
        int tagIndex = triggerObservations.FindIndex((f) => obj.CompareTag(f.tag));

        if (tagIndex == -1) return; // not observing

        triggerObservations[tagIndex].objects.RemoveAt(tagIndex);
    }

    public List<GameObject> GetObservationsByTag(string tag)
    {
        return triggerObservations.Find((f) => f.tag == tag).objects;
    }
}
