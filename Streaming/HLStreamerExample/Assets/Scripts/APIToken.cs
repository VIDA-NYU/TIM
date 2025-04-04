using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class APIToken
{
    public string access_token;
    public string token_type;
    public static APIToken FromJSON(string json)
    {
        return JsonUtility.FromJson<APIToken>(json);
    }
}