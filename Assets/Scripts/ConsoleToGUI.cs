using UnityEngine;

public class ConsoleToGUI : MonoBehaviour
{
    string myLog = "*begin log";
    string filename = "";
    bool doShow = true;
    int kChars = 700;

    void OnEnable() 
    { 
        Application.logMessageReceived += Log; 
    }

    void OnDisable() 
    { 
        Application.logMessageReceived -= Log; 
    }

    void Update() 
    { 
        if (Input.GetKeyDown(KeyCode.Space)) 
        { 
            doShow = !doShow; 
        } 
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        // Extract the first line of the stack trace, if available, which includes the file and line number
        string lineInfo = "";
        if (!string.IsNullOrEmpty(stackTrace))
        {
            string[] lines = stackTrace.Split('\n');
            if (lines.Length > 0)
            {
                lineInfo = lines[0];  // This line usually contains the file and line number
            }
        }

        // Format the log entry with line information
        string formattedLog = $"{logString} ({lineInfo})";

        // For onscreen display
        myLog += "\n" + formattedLog;

        if (myLog.Length > kChars) 
        { 
            myLog = myLog.Substring(myLog.Length - kChars); 
        }

        // For file output
        if (filename == "")
        {
            string d = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop) + "/YOUR_LOGS";
            System.IO.Directory.CreateDirectory(d);
            string r = Random.Range(1000, 9999).ToString();
            filename = d + "/log-" + r + ".txt";
        }

        try 
        { 
            System.IO.File.AppendAllText(filename, formattedLog + "\n"); 
        }
        catch { }
    }

    void OnGUI()
    {
        if (!doShow) 
        { 
            return; 
        }

        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
        new Vector3(Screen.width / 1200.0f, Screen.height / 800.0f, 1.0f));
        GUI.TextArea(new Rect(10, 10, 540, 370), myLog);
    }
}
