using System.Collections.Generic;
using UnityEngine;

/*
 Instead of Instantiate() use ObjectPoolz.Instance.Spawn();
 Instead of Destroy() use ObjectPoolz.Instance.Return(this.GameObject);
 And it should just work™
 */

public class ObjectPoolz : MonoBehaviour
{
    [Header("Debugging")]
    public bool showDebugInfo;
    public float debugYOffset = 0;
    
    private GUIStyle _textStyle = new GUIStyle();
    private string _debugTxt;
    
    private int _objInstantiated = 0;
    private int _objFree = 0;
    
    public static ObjectPoolz Instance { get; private set; }

    private HashSet<string> poolKeys = new HashSet<string>();
    private Dictionary<string, List<GameObject>> pool = new Dictionary<string, List<GameObject>>();
    private Dictionary<string, Vector3> poolLocalScales = new Dictionary<string, Vector3>();

    private bool PoolHasKey(string s)
    {
        return poolKeys.Contains(s); // faster to check if a HashSet contains something
    }

    GameObject InstantiateNew(GameObject prefab)
    {
        GameObject newObj = Instantiate(prefab, transform);
        newObj.name = prefab.name; // to remove "(Clone)"
        _objInstantiated += 1;
        _objFree += 1;
        return newObj;
    }

    void PrepareForNewPrefab(GameObject prefab)
    {
        poolKeys.Add(prefab.name);
        pool.Add(prefab.name, new List<GameObject>());

        pool[prefab.name].Add(InstantiateNew(prefab));
        poolLocalScales.Add(prefab.name, pool[prefab.name][0].transform.localScale);
    }
    
    GameObject GetFreeObject(GameObject prefab)
    {
        if (!PoolHasKey(prefab.name))
        {
            PrepareForNewPrefab(prefab);
        }

        if (pool[prefab.name].Count == 0)
        {
            pool[prefab.name].Add(InstantiateNew(prefab));
        }
        
        
        GameObject freeObject = pool[prefab.name][0];
        pool[prefab.name].RemoveAt(0);
        _objFree -= 1;
        return freeObject;
    }

    public GameObject Spawn(GameObject prefab, Vector3 position = default, Quaternion rotation = default, Transform parent = default, bool active = true)
    {
        GameObject GO = GetFreeObject(prefab);

        if (parent == default)
        {
            parent = transform;
        }

        GO.transform.position = position;
        GO.transform.rotation = rotation;
        GO.transform.parent = parent;
        GO.SetActive(active);
        return GO;
    }

    public void Return(GameObject returningObject)
    {
        _objFree += 1;
        string poolName = returningObject.name;
        returningObject.SetActive(false);
        returningObject.transform.localScale = poolLocalScales[poolName]; // restore original scale
        pool[poolName].Add(returningObject);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(this);
        }

        _textStyle.fontStyle = FontStyle.Bold;
        _textStyle.normal.textColor = Color.red;
    }

    void OnGUI()
    {
        if (showDebugInfo)
        {
            _debugTxt = "(" + this.gameObject.name + ") ";
            _debugTxt += "instantiated: " + _objInstantiated + " ";
            _debugTxt += "(active: " + (_objInstantiated - _objFree) + ", free: " + _objFree + ")";
            foreach (string s in pool.Keys)
            {
                _debugTxt += "\n" + s + " = " + pool[s].Count + " free";
            }
            
            GUI.Label(new Rect(5, debugYOffset, 100, 25),
                _debugTxt,
                _textStyle);
        }
    }
}
