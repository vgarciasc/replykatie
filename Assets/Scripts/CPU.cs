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

    public static IntentProcess Proc_SearchFood(float priority) {
        return new IntentProcess(
            priority,
            IntentProcessKind.SEARCH_FOOD,
            new List<IntentProcessKind>() { IntentProcessKind.GO_FOOD }
        );
    }

    public static IntentProcess Proc_GoFood(float priority, GameObject obj) {
        return new IntentProcess(
            priority,
            IntentProcessKind.GO_FOOD,
            new List<IntentProcessKind>() { IntentProcessKind.EAT_FOOD },
            obj.transform.position,
            obj
        );
    }

    public static IntentProcess Proc_EatFood(float priority, GameObject obj) {
        return new IntentProcess(
            priority,
            IntentProcessKind.EAT_FOOD,
            null,
            obj.transform.position,
            obj
        );
    }

    public static IntentProcess Proc_SearchMate(float priority) {
        return new IntentProcess(
            priority,
            IntentProcessKind.SEARCH_MATE,
            new List<IntentProcessKind>() { IntentProcessKind.GO_MATE }
        );
    }

    public static IntentProcess Proc_GoMate(float priority, GameObject obj) {
        return new IntentProcess(
            priority,
            IntentProcessKind.GO_MATE,
            new List<IntentProcessKind>() { IntentProcessKind.COPULATE },
            obj.transform.position,
            obj
        );
    }

    public static IntentProcess Proc_Copulate(float priority, GameObject obj) {
        return new IntentProcess(
            priority,
            IntentProcessKind.COPULATE,
            null,
            obj.transform.position,
            obj
        );
    }

    public static IntentProcess Proc_Idle() {
        return new IntentProcess(
            1,
            IntentProcessKind.IDLE,
            null
        );
    }
}

// Creature Processes Unit
public class CPU : MonoBehaviour
{
    [SerializeField]
    IntentProcess currentProcess = null;

    Coroutine currentProcessCoroutine;

    Creature creature;

    [SerializeField]
    bool verbose = false;

    void Start()
    {
        this.creature = this.GetComponent<Creature>();
    }

    public void ResetState() {
        this.currentProcess = null;
        if (this.currentProcessCoroutine != null) {
            StopCoroutine(this.currentProcessCoroutine);
        }
    }
    
    public void Interrupt(IntentProcess process) {
        if (ShouldUpdateProcess(process.kind)) {
            currentProcess.priority = process.priority;
            return;
        }
        
        if (ShouldInterruptProcess(process.priority, process.kind)) {
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
        this.currentProcess = process;
        this.currentProcessCoroutine = StartCoroutine(processCoroutine);
    }

    void ChangeCurrentProcess(IntentProcess process) {
        if (verbose && currentProcess != null && process != null) {
            print(">> Change of process. ["
                + System.Enum.GetName(typeof(IntentProcessKind), currentProcess.kind)
                + "] (" + currentProcess.priority + ") => ["
                + System.Enum.GetName(typeof(IntentProcessKind), process.kind)
                + "] (" + process.priority + ")");
        }

        CancelCurrentProcess();
        StartNewProcess(process);
    }

    bool ShouldInterruptProcess(float priority, IntentProcessKind kind) {
        return (currentProcess == null
            || currentProcess.priority < priority
            || (currentProcess.chainStates != null
                && currentProcess.chainStates.Contains(kind)));
    }

    bool ShouldUpdateProcess(IntentProcessKind kind) {
        return currentProcess != null
            && currentProcess.kind == kind;
    }
}
