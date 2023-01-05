using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonBehavior<T> : MonoBehaviour where T : SingletonBehavior<T>
{
    protected static T instance;
    public static T Instance 
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<T>();
                if (instance && instance.Persistant)
                    DontDestroyOnLoad(instance.gameObject);
            }

            return instance;
        }
    }
    protected virtual bool Persistant => false;
    //139
}
