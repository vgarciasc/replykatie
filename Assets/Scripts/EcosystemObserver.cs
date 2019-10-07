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

    }

    void Update()
    {
        rabbitCounter.text = GameObject.FindObjectsOfType<Creature>().Length.ToString();
        flowerCounter.text = GameObject.FindObjectsOfType<Plant>().Length.ToString();
    }
}
