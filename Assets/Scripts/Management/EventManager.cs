using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
[System.Serializable]
public class EventListenerInfo
{
    public string eventName;
    public int listenerCount;
    public List<string> listenerTargets = new List<string>();

    public EventListenerInfo(string eventName, int listenerCount, List<string> listenerTargets)
    {
        this.eventName = eventName;
        this.listenerCount = listenerCount;
        this.listenerTargets = listenerTargets;
    }
}

[CreateAssetMenu(fileName = "EventManager", menuName = "Game/Managers/EventManager")]
public class EventManager : ScriptableObject
{

    private Dictionary<string, Action<EventParam>> eventDictionary = new Dictionary<string, Action<EventParam>>();

    [Header("Inspector Debug Info")]
    [SerializeField, Tooltip("Shows all currently active event listeners (Runtime Only)")]
    private List<EventListenerInfo> activeListeners = new List<EventListenerInfo>();

    public static EventManager instance => GameManager.Instance.eventManager;

    public void OnApplicationPause()
    {
    }
    
    public void OnApplicationQuit()
    {
        ClearAllListeners();
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        // Clear listeners when exiting play mode
        if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode && UnityEditor.EditorApplication.isPlaying)
        {
            ClearAllListeners();
        }
#endif
    }

    private void ClearAllListeners()
    {
        eventDictionary.Clear();
        activeListeners.Clear();
    }

    public static void StartListening(string eventName, Action<EventParam> listener)
    {
        Action<EventParam> thisEvent;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            //Add more event to the existing one
            thisEvent += listener;

            //Update the Dictionary
            instance.eventDictionary[eventName] = thisEvent;
        }
        else
        {
            //Add event to the Dictionary for the first time
            thisEvent += listener;
            instance.eventDictionary.Add(eventName, thisEvent);
        }
    }

    public static void StartListening(GameEvent eventName, Action<EventParam> listener)
    {
        StartListening(eventName.ToString(), listener);
    }

    public static void StopListening(string eventName, Action<EventParam> listener)
    {
        if (GameManager.Instance.eventManager == null) return;
        Action<EventParam> thisEvent;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            //Remove event from the existing one
            thisEvent -= listener;

            //Update the Dictionary
            instance.eventDictionary[eventName] = thisEvent;
        }
    }

    public static void StopListening(GameEvent eventName, Action<EventParam> listener)
    {
        StopListening(eventName.ToString(), listener);
    }

    public static void TriggerEvent(string eventName)
    {
        EventParam eventParam = new EventParam();
        Action<EventParam> thisEvent = null;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            if(thisEvent == null)
            {
                Debug.LogWarning($"Event '{eventName}' has some null listener(s). This might result events to notfiy its listeners correctly");
            }
            thisEvent.Invoke(eventParam);
            // OR USE  instance.eventDictionary[eventName](eventParam);
        }
    }

    public static void TriggerEvent(GameEvent eventName)
    {
        TriggerEvent(eventName.ToString());
    }

    public static void TriggerEvent(string eventName, EventParam eventParam)
    {
        Action<EventParam> thisEvent = null;
        if (instance.eventDictionary.TryGetValue(eventName, out thisEvent))
        {
            thisEvent.Invoke(eventParam);
            // OR USE  instance.eventDictionary[eventName](eventParam);
        }
    }

    public static void TriggerEvent(GameEvent eventName, EventParam eventParam)
    {
        TriggerEvent(eventName.ToString(), eventParam);
    }

}

[System.Serializable]
public class EventParam
{
    public GameObject paramObj;
    public ScriptableObject paramScriptable;
    public int paramInt;
    public float paramFloat;
    public string paramStr;
    public Type paramType;
    public Vector3[] vectorList;
    public bool paramBool;
    public Dictionary<string, object> paramDictionary;

    public EventParam()
    {

    }

    public EventParam(Dictionary<string, object> paramDictionary)
    {
        this.paramDictionary = paramDictionary;
    }

    public EventParam(GameObject paramObj = null, ScriptableObject paramScriptable = null, int paramInt = 0, float paramFloat = 0f, string paramStr = "", Type paramType = null, Dictionary<string, object> paramDictionary = null,
    Vector3[] vectorList = null, bool paramBool = false)
    {
        this.paramObj = paramObj;
        this.paramScriptable = paramScriptable;
        this.paramInt = paramInt;
        this.paramFloat = paramFloat;
        this.paramStr = paramStr;
        this.paramType = paramType;
        this.paramDictionary = paramDictionary;
        this.vectorList = vectorList;
        this.paramBool = paramBool;
    }
    }
}