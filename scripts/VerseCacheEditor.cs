using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VerseCache))]
public class VerseCacheEditor : Editor
{
    private const string InOutFilepathKey = "VerseCache_InOutFilepath";
    private const string CachePathKey = "VerseCache_CachePath";
    private string defaultDataPath;
    
    public override void OnInspectorGUI()
    {
        VerseCache myScript = (VerseCache)target;

        if (GUILayout.Button("List Files in Cache"))
        {
            myScript.CacheCheck();
            myScript.ListFiles();
        }

        if (GUILayout.Button("Put/Get"))
        {
            myScript.CacheCheck();
            myScript.DoAction();
        }

        DrawDefaultInspector();

        if (GUILayout.Button("Generate Key & IV"))
        {
            myScript.CacheCheck();
            myScript.GenerateRandomKeyIV();
        }
    }

    private void OnEnable()
    {
        VerseCache myScript = (VerseCache)target;

        if (string.IsNullOrEmpty(myScript.InOutFilepath))
        {
            myScript.InOutFilepath = Application.dataPath;
        }

        if (string.IsNullOrEmpty(myScript.cachePath))
        {
            myScript.cachePath = Application.persistentDataPath;
        }
    }
    private string GetInoutFilePath()
    {
        string path = EditorPrefs.GetString(InOutFilepathKey);
        if (string.IsNullOrEmpty(path))
        {
            path = defaultDataPath;
            EditorPrefs.SetString(InOutFilepathKey, path);
        }
        return path;
    }
    private string GetCachePath()
    {
        string path = EditorPrefs.GetString(CachePathKey);
        if (string.IsNullOrEmpty(path))
        {
            path = Application.persistentDataPath;
            EditorPrefs.SetString(CachePathKey, path);
        }
        return path;
    }
}
