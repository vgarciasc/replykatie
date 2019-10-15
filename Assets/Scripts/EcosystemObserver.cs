using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EcosystemObserver : MonoBehaviour
{
    public static EcosystemObserver GetEcosystemObserver() {
        return (EcosystemObserver) HushPuppy.safeFindComponent("GameController", "EcosystemObserver");
    }

    [SerializeField]
    TextMeshProUGUI rabbitCounter;
    [SerializeField]
    TextMeshProUGUI flowerCounter;

    void Start()
    {
        StartCoroutine(ObserveGenes());
    }

    void Update()
    {
        rabbitCounter.text = GameObject.FindObjectsOfType<Creature>().Length.ToString();
        flowerCounter.text = GameObject.FindObjectsOfType<ConstantFlower>().Length.ToString();
    }

    IEnumerator ObserveGenes() {
        while (true) {
            var creatures = new List<Creature>(
                GameObject.FindObjectsOfType<Creature>());

            var viewDistanceMean = 0f;
            creatures.ForEach((c) => {viewDistanceMean += c.viewDistance;});
            viewDistanceMean /= creatures.Count;

            var baseSpeedMean = 0f;
            creatures.ForEach((c) => {baseSpeedMean += c.baseSpeed;});
            baseSpeedMean /= creatures.Count;

            print("[view distance]: {" + viewDistanceMean + "} ; [base speed]: {" + baseSpeedMean + "}");
            
            yield return new WaitForSeconds(5f);
        }
    }
}
