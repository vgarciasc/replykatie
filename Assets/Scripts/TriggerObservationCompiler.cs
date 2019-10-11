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
        if (tagIndex == -1) return; // not observing this tag

        int objIndex = triggerObservations[tagIndex].objects.FindIndex((f) => f == obj);
        if (objIndex != -1) return; // already observed this object

        triggerObservations[tagIndex].objects.Add(obj);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        var obj = collision.gameObject;

        int tagIndex = triggerObservations.FindIndex((f) => obj.CompareTag(f.tag));
        if (tagIndex == -1) return; // not observing

        int objIndex = triggerObservations[tagIndex].objects.FindIndex((f) => f == obj);
        if (objIndex == -1) {
            // Debug.LogError("This should not be happening.");
            // print(obj + " wasn't observed, but exited");
            // Debug.Break();
            return; // not observed this object
        }

        triggerObservations[tagIndex].objects.RemoveAt(objIndex);
    }

    public List<GameObject> GetObservationsByTag(string tag)
    {
        return triggerObservations.Find((f) => f.tag == tag).objects;
    }

    public void CleanObservations()
    {
        foreach (var triggerObservation in this.triggerObservations)
        {
            triggerObservation.objects = triggerObservation.objects.FindAll((f) => f.activeSelf);
        }
    }
}
