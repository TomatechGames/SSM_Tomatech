using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TriggerTracker : MonoBehaviour
{
    [SerializeField]
    LayerMask layerFilter;
    [SerializeField]
    int activationThreshold = 1;
    List<Collider2D> trackedColliders = new();
    public UnityEvent onActivate;
    public UnityEvent onDeactivate;
    public UnityEvent<int> onCountChange;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (MatchesFilter(collision) && !trackedColliders.Contains(collision))
        {
            int prevCount = trackedColliders.Count;
            trackedColliders.Add(collision);
            UpdateTrackedContents(prevCount);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (MatchesFilter(collision) && trackedColliders.Contains(collision))
        {
            int prevCount = trackedColliders.Count;
            trackedColliders.Remove(collision);
            UpdateTrackedContents(prevCount);
        }
    }

    bool MatchesFilter(Collider2D collision) => layerFilter == (layerFilter | (1 << collision.gameObject.layer));

    void UpdateTrackedContents(int prevCount)
    {
        if(prevCount>=activationThreshold && trackedColliders.Count<activationThreshold)
        {
            //turn off
            onDeactivate.Invoke();
        }
        else if(prevCount<activationThreshold && trackedColliders.Count>=activationThreshold)
        {
            //turn on
            onActivate.Invoke();
        }
        onCountChange.Invoke(trackedColliders.Count);
    }
}
