using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

//https://www.youtube.com/watch?v=Pi4SHO0IEQY
public class DebugDisplay : MonoBehaviour
{
    private TextMeshProUGUI text;
    private Dictionary<string, string> debugLogs = new Dictionary<string, string>();

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        Application.logMessageReceived += HandleLog;
        Debug.Log("log test");
    }

    private void OnEnable()
    {
        text = GetComponent<TextMeshProUGUI>();
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logStr, string stackTrace, LogType type)
    {
        if(type == LogType.Log)
        {
            string[] splitStr = logStr.Split(char.Parse(":"));
            string debugKey = splitStr[0];
            string debugValue = splitStr.Length > 1 ? splitStr[1] : "";

            if (debugLogs.ContainsKey(debugKey))
            {
                debugLogs[debugKey] = debugValue;
            }
            else
            {
                debugLogs.Add(debugKey, debugValue);
            }
        }

        string displayText = "";
        foreach(KeyValuePair<string, string> log in debugLogs)
        {
            if(log.Value == "")
            {
                displayText += log.Key + "\n";
            }
            else
            {
                displayText += log.Key + ": " + log.Value + "\n";
            }
            text.text = displayText;
        }

    }
}
