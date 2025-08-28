using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A utility class to run code on Unity's main thread from other threads.
/// This version uses Awake for initialization to be thread-safe.
/// </summary>
public class MainThreadDispatcher : MonoBehaviour
{
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static MainThreadDispatcher _instance = null;

    // Public property to access the instance
    public static MainThreadDispatcher Instance
    {
        get { return _instance; }
    }

    void Awake()
    {
        // Ensure there is only one instance of this dispatcher
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject); // Ensure it persists across scene loads
        }
    }

    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }

    /// <summary>
    /// Queues an action to be executed on the main thread.
    /// </summary>
    /// <param name="action">The action to be executed.</param>
    public void Enqueue(Action action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(action);
        }
    }
}