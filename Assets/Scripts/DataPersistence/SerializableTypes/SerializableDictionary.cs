using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{

    [SerializeField] private List<TKey>   keys   = new List<TKey>();
    [SerializeField] private List<TValue> values = new List<TValue>();

    // save the dictionary to 2 lists
    public void OnBeforeSerialize()
    {
        keys.Clear();
        values.Clear();
        foreach (KeyValuePair<TKey, TValue> pair in this) 
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
    }

    // load the dictionary from 2 lists
    public void OnAfterDeserialize()
    {
        this.Clear();

        if (keys.Count != values.Count) 
        {
            Debug.LogError("Tried to deserialize Dictionary, amount of keys ("+ keys.Count + ") != number of values (" + values.Count + "), something went wrong");
        }

        for (int i = 0; i < keys.Count; i++) 
        {
            this.Add(keys[i], values[i]);
        }
    }

}
