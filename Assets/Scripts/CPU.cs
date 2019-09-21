using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum IntentProcessKind {
    SEARCH_FOOD, GO_FOOD, EAT_FOOD,
    SEARCH_MATE, GO_MATE, COPULATE,
    SEARCH_WATER, GO_WATER, DRINK_WATER,
    RUN_PREDATOR,
    IDLE
}

[System.Serializable]
public class IntentProcess {
    public float priority;
    public IntentProcessKind kind;
    public List<IntentProcessKind> chainStates = new List<IntentProcessKind>();
    public Vector3 position;
    public GameObject target;

    public IntentProcess(float priority, IntentProcessKind kind, List<IntentProcessKind> chainStates) {
        this.priority = priority;
        this.kind = kind;
        this.chainStates = chainStates;
    }

    public IntentProcess(float priority, IntentProcessKind kind, List<IntentProcessKind> chainStates,
        Vector3 position, GameObject target) {
        this.priority = priority;
        this.kind = kind;
        this.chainStates = chainStates;
        this.position = position;
        this.target = target;
    }
}

// Creature Processes Unit
public class CPU : MonoBehaviour
{
    [SerializeField]
    IntentProcess currentProcess = null;

    Coroutine currentProcessCoroutine;

    Creature creature;

    void Start()
    {
        this.creature = this.GetComponent<Creature>();
    }

    void Update()
    {
        
    }
    
    public void Interrupt(IntentProcess process) {
        if (currentProcess != null
            && currentProcess.kind == process.kind) {
            currentProcess.priority = process.priority;
            return;
        }
        
        if (currentProcess == null
            || currentProcess.priority < process.priority
            || (currentProcess.chainStates != null
                && currentProcess.chainStates.Contains(process.kind))) {
            ChangeCurrentProcess(process);
        }
    }

    public void EndProcess() {
        CancelCurrentProcess();
    }

    void CancelCurrentProcess() {
        if (this.currentProcessCoroutine != null) {
            StopCoroutine(currentProcessCoroutine);
            this.currentProcessCoroutine = null;
        }

        this.currentProcess = null;
        creature.CleanContext();
    }

    void StartNewProcess(IntentProcess process) {
        var processCoroutine = creature.GetProcessCoroutine(process);
        this.currentProcessCoroutine = StartCoroutine(processCoroutine);
        this.currentProcess = process;
    }

    void ChangeCurrentProcess(IntentProcess process) {
        if (currentProcess != null && process != null) {
            print(">> Change of process. ["
                + System.Enum.GetName(typeof(IntentProcessKind), currentProcess.kind)
                + "] => ["
                + System.Enum.GetName(typeof(IntentProcessKind), process.kind)
                + "]");
        }

        CancelCurrentProcess();
        StartNewProcess(process);
    }
}
