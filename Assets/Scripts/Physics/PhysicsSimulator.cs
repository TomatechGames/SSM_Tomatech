using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class PhysicsSimulator : SingletonBehavior<PhysicsSimulator>
{
    const int SIMS_PER_SECOND = 500;
    public const float SIM_DELTA_TIME = 1f / SIMS_PER_SECOND;

    //const int SIMULATION_SUBDIVISIONS = 10;
    //const float SIMULATION_INCREMENT = 1f / SIMULATION_SUBDIVISIONS;

    //public static float SIM_DELTA_TIME => SIMULATION_INCREMENT * Time.fixedDeltaTime;

    //event Action Simulate;

    //[SerializeField]
    //float debugSimulationRate;
    //[SerializeField]
    //long simulationCount;
    [SerializeField]
    float deltaTimeRemainder;
    List<ISimulatable> registeredSimulatables = new();
    List<ISimulatable> simsToAdd = new();
    List<ISimulatable> simsToRemove = new();
    bool simulationActive = false;

    ///might be unneccecary
    private void Awake()
    {
        registeredSimulatables.Clear();
    }

    private void Update()
    {
        float totalDeltaTime = Time.deltaTime + deltaTimeRemainder;
        int simCount = Mathf.FloorToInt(totalDeltaTime / SIM_DELTA_TIME);
        deltaTimeRemainder = totalDeltaTime - (simCount * SIM_DELTA_TIME);

        simulationActive = true;
        for (int i = 0; i < simCount; i++)
        {
            foreach (var simulatable in registeredSimulatables)
            {
                if (!simsToRemove.Contains(simulatable))
                    simulatable.Simulate();
            }
            //simulationCount++;
        }
        simulationActive = false;
        foreach (var item in simsToAdd)
        {
            registeredSimulatables.Add(item);
        }
        simsToAdd.Clear();
        foreach (var item in simsToRemove)
        {
            registeredSimulatables.Remove(item);
        }
        simsToRemove.Clear();

        //debugSimulationRate = simulationCount / Time.time;
    }

    //old simulation logic
    //void FixedUpdate()
    //{
    //    for (int i = 0; i < SIMULATION_SUBDIVISIONS; i++)
    //    {
    //        Simulate?.Invoke();
    //        simulationCount++;
    //    }
    //    debugSimulationRate = simulationCount / Time.time;
    //}

    public void RegisterSimulatable(ISimulatable simulatable)
    {
        if (simulationActive)
            simsToAdd.Add(simulatable);
        else
            registeredSimulatables.Add(simulatable);
    }

    public void UnregisterSimulatable(ISimulatable simulatable)
    {
        if (simulationActive)
            simsToRemove.Add(simulatable);
        else
            registeredSimulatables.Remove(simulatable);
    }
}

public interface ISimulatable
{
    public void Simulate();
    public bool SimulationActive => true;
}
