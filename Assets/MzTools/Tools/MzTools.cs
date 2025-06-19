/******************************************************************************
 *
 * Maintaince Logs:
 * 2012-11-11   WP      Initial version. 
 * 2012-12-02   WP      Added methods of AddChild
 * 2012-12-15   WP      Added methods.(RoundToDecimal,RoundToDecByObj)
 * 2013-03-07   WP      Added methods.（FindInParents,FindActive)
 * 2013-03-29   Waigo   Added Decrypt.Encrypt
 * 2013-08-27   WP      Fixed Verison To U_4.2.0f
 * 2013-09-05   WP      Added AddItemToList^^^^^^^^^^^ ,two 
 * 2014-11-14   WP      Added string convert to Vector3
 * 2015-01-15   WP      Added Destroy Children by gameObject
 * 2016-06-24   WP      Added convert pos by cameras
 * 
 * *****************************************************************************/

using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Security.Cryptography;
using System.IO;

static public class MzTools
{

    /// <summary>
    /// Determines whether the 'parent' contains a 'child' in its hierarchy.
    /// </summary>

    static public bool IsChild(Transform parent, Transform child)
    {
        if (parent == null || child == null) return false;

        while (child != null)
        {
            if (child == parent) return true;
            child = child.parent;
        }
        return false;
    }

    #region Set active or deactivate

    /// <summary>
    /// Activate the specified object and all of its children.
    /// </summary>

    static void Activate(Transform t)
    {
        SetActiveSelf(t.gameObject, true);

        // Prior to Unity 4, active state was not nested. It was possible to have an enabled child of a disabled object.
        // Unity 4 onwards made it so that the state is nested, and a disabled parent results in a disabled child.
#if UNITY_3_5
		for (int i = 0, imax = t.GetChildCount(); i < imax; ++i)
		{
			Transform child = t.GetChild(i);
			Activate(child);
		}
#else
        // If this point is reached, then all the children are disabled, so we must be using a Unity 3.5-based active state scheme.
        for (int i = 0, imax = t.childCount; i < imax; ++i)
        {
            Transform child = t.GetChild(i);
            Activate(child);
        }
#endif
    }

    /// <summary>
    /// Deactivate the specified object and all of its children.
    /// </summary>

    static void Deactivate(Transform t)
    {
#if UNITY_3_5
		for (int i = 0, imax = t.GetChildCount(); i < imax; ++i)
		{
			Transform child = t.GetChild(i);
			Deactivate(child);
		}
#endif
        SetActiveSelf(t.gameObject, false);
    }

    /// <summary>
    /// SetActiveRecursively enables children before parents. This is a problem when a widget gets re-enabled
    /// and it tries to find a panel on its parent.
    /// </summary>

    static public void SetActive(GameObject go, bool state)
    {
        if (state)
        {
            Activate(go.transform);
        }
        else
        {
            Deactivate(go.transform);
        }
    }

    /// <summary>
    /// Activate or deactivate children of the specified game object without changing the active state of the object itself.
    /// </summary>

    static public void SetActiveChildren(GameObject go, bool state)
    {
        Transform t = go.transform;

        if (state)
        {
            for (int i = 0, imax = t.childCount; i < imax; ++i)
            {
                Transform child = t.GetChild(i);
                Activate(child);
            }
        }
        else
        {
            for (int i = 0, imax = t.childCount; i < imax; ++i)
            {
                Transform child = t.GetChild(i);
                Deactivate(child);
            }
        }
    }

    /// <summary>
    /// Unity4 has changed GameObject.active to GameObject.activeself.
    /// </summary>

    static public bool GetActive(GameObject go)
    {
#if UNITY_3_5
        return go && go.active;
#else
        return go && go.activeInHierarchy;
#endif
    }

    /// <summary>
    /// Unity4 has changed GameObject.active to GameObject.SetActive.
    /// </summary>

    static public void SetActiveSelf(GameObject go, bool state)
    {
#if UNITY_3_5
        go.active = state;
#else
        go.SetActive(state);
#endif
    }

    #endregion

    #region Add GameObject or Component

    static public T AddChild<T>(GameObject parent, T prefab, bool isChangeLayer = true, bool isPreSize = false) where T : Component
    {
        T go = Component.Instantiate(prefab) as T;

        if (go != null && parent != null)
        {
            Transform t = go.gameObject.transform;
            if (t is RectTransform)
            {
                RectTransform rt = t as RectTransform;
                rt.SetParent(parent.transform);

                RectTransform prbRt = prefab.transform as RectTransform;
                if (prbRt)
                {
                    rt.offsetMax = prbRt.offsetMax;
                    rt.offsetMin = prbRt.offsetMin;
                }
            }
            else
                t.parent = parent.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            if (!isPreSize) t.localScale = Vector3.one;
            else t.localScale = prefab.transform.localScale;
            //else t.localScale = prefab.transform.localScale;
            if (isChangeLayer) SetLayer(go.gameObject, parent.layer);
        }
        return go;
    }

    /// <summary>
    /// create prefab under perent
    /// </summary>
    /// <param name="parent">parent</param>
    /// <param name="prefab">prefab</param>
    /// <param name="isChangeLayer">change layer with parent</param>
    /// <param name="isPosAndRot">reset position and rotation</param>
    /// <param name="isPreSize">set size with prefab</param>
    /// <returns></returns>
    static public T AddChild<T>(GameObject parent, T prefab, bool isChangeLayer, bool isPosAndRot, bool isPreSize) where T : Component
    {
        T go = Component.Instantiate(prefab) as T;

        if (go != null && parent != null)
        {
            Transform t = go.gameObject.transform;
            if (t is RectTransform)
            {
                RectTransform rt = t as RectTransform;
                rt.SetParent(parent.transform);

                RectTransform prbRt = prefab.transform as RectTransform;
                if (prbRt)
                {
                    rt.offsetMax = prbRt.offsetMax;
                    rt.offsetMin = prbRt.offsetMin;
                }
            }
            else
                t.parent = parent.transform;
            if (isPosAndRot)
            {
                t.localPosition = Vector3.zero;
                t.localRotation = Quaternion.identity;
            }
            else
            {
                t.localPosition = prefab.transform.localPosition;
                t.localRotation = prefab.transform.localRotation;
            }
            if (!isPreSize) t.localScale = Vector3.one;
            if (isChangeLayer) SetLayer(go.gameObject, parent.layer);
        }
        return go;
    }

    static public T AddChildByClone<T>(T prefab, bool isChangeLayer = true, bool isPreSize = false) where T : MonoBehaviour
    {
        GameObject parent = null;

        if (prefab.transform != null)
            parent = prefab.transform.gameObject;

        T t = AddChild(prefab.transform.parent.gameObject, prefab, isChangeLayer, isPreSize);
        t.transform.localPosition = prefab.transform.localPosition;
        t.transform.localRotation = prefab.transform.localRotation;

        return t;
    }

    /// <summary>
    /// create Prefab under parent
    /// </summary>
    static public GameObject AddGameObj(GameObject parent, GameObject prefab, bool isChangeLayer, bool isPreSize = false)
    {
        GameObject go = GameObject.Instantiate(prefab) as GameObject;

        if (go != null && parent != null)
        {
            Transform t = go.transform;
            if (t is RectTransform)
            {
                RectTransform rt = t as RectTransform;
                rt.SetParent(parent.transform);

                RectTransform prbRt = prefab.transform as RectTransform;
                if (prbRt)
                {
                    rt.offsetMax = prbRt.offsetMax;
                    rt.offsetMin = prbRt.offsetMin;
                }
            }
            else
                t.parent = parent.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            if (!isPreSize) t.localScale = Vector3.one;
            if (isChangeLayer) SetLayer(go.gameObject, parent.layer);
        }
        return go;
    }

    /// <summary>
    /// create GameObject
    /// </summary>
    static public GameObject AddGameObj(GameObject parent)
    {
        GameObject go = new GameObject();

        if (go != null && parent != null)
        {
            Transform t = go.transform;
            if (parent.transform is RectTransform)
            {
                t = go.AddComponent<RectTransform>();
                t.SetParent(parent.transform);
                //                Debug.Log("set the " + t is RectTransform);
            }
            else
                t.parent = parent.transform;
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            SetLayer(go.gameObject, parent.layer);
        }
        return go;
    }

    #endregion

    /// <summary>
    /// Recursively set the game object's layer.
    /// </summary>

    static public void SetLayer(GameObject go, int layer)
    {
        go.layer = layer;

        Transform t = go.transform;

        for (int i = 0, imax = t.childCount; i < imax; ++i)
        {
            Transform child = t.GetChild(i);
            SetLayer(child.gameObject, layer);
        }
    }

    /// <summary>
    /// Will a floating-point point accurately to small points
    /// 
    /// </summary>
    /// <param name="param"></param>
    /// <param name="toInt"></param>
    /// <returns></returns>
    public static float RoundToDecimal(float param, int toInt)
    {
        if (toInt < 0) return param;
        param = Mathf.RoundToInt(param * Mathf.Pow(10, toInt)) / Mathf.Pow(10, toInt);
        return param;
    }

    /// <summary>
    /// Round position and rotation and localScale of one GameObject!
    /// 
    /// </summary>
    public static void RoundToDecByObj(GameObject go)
    {
        RoundLocalPosition(go);
        RoundLocalEulerAngles(go);
        RoundLocalScale(go);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="go"></param>
    public static void RoundLocalPosition(GameObject go)
    {
        Vector3 pos = go.transform.localPosition;
        pos.x = Mathf.RoundToInt(pos.x);
        pos.y = Mathf.RoundToInt(pos.y);
        pos.z = Mathf.RoundToInt(pos.z);
        go.transform.localPosition = pos;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="go"></param>
    public static void RoundLocalEulerAngles(GameObject go)
    {
        Vector3 rot = go.transform.localEulerAngles;
        rot.x = Mathf.RoundToInt(rot.x);
        rot.y = Mathf.RoundToInt(rot.y);
        rot.z = Mathf.RoundToInt(rot.z);
        go.transform.localEulerAngles = rot;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="go"></param>
    public static void RoundLocalScale(GameObject go)
    {
        Vector3 loc = go.transform.localScale;
        if (Mathf.RoundToInt(loc.x) != 0) loc.x = Mathf.RoundToInt(loc.x);
        if (Mathf.RoundToInt(loc.y) != 0) loc.y = Mathf.RoundToInt(loc.y);
        if (Mathf.RoundToInt(loc.z) != 0) loc.z = Mathf.RoundToInt(loc.z);
        go.transform.localScale = loc;
    }

    /// <summary>
    /// Will z of localPosition of the GameObject Change to zPos
    /// </summary>
    public static void ChangeZOfLocalPosition(Transform t, float zPos = 0)
    {
        Vector3 pos = t.localPosition;
        pos.z = zPos;
        t.localPosition = pos;
    }

    /// <summary>
    /// ,odds = 0 ~ 100,such as %60 -> odds = 60 .
    /// </summary>
    public static bool OddsByFloat(float odds)
    {
        GenerateSeed();
        //UnityEngine.Random.InitState((int)Time.time);
        if (odds <= 0) return false;
        else if (odds >= 100) return true;
        if (UnityEngine.Random.Range(0, 10000) < odds * 100)
        {
            return true;
        }
        else return false;
    }

    /// <summary>
    /// 
    /// 
    /// </summary>
    /// <param name="odds">Odds.</param>
    public static int Odds(params float[] odds)
    {
        //UnityEngine.Random.InitState((int)Time.time);
        GenerateSeed();
        int index = odds.Length;
        float odd = UnityEngine.Random.Range(0, 10000f);

        if (odds.Length > 0)
        {
            float cur = odds[0] * 100;
            if (odd < cur)
            {
                index = 0;
                //Debug.Log(odd + " ----- " + cur);
            }
            else
                for (int i = 1; i < odds.Length; i++)
                {
                    cur += odds[i] * 100;
                    if (odd < cur)
                    {
                        index = i;
                        break;
                    }
                }
        }

        return index;
    }
    public static int Odds(List<float> odds)
    {
        //UnityEngine.Random.InitState((int)Time.time);
        GenerateSeed();
        int index = odds.Count;
        float odd = UnityEngine.Random.Range(0, 10000f);

        if (odds.Count > 0)
        {
            float cur = odds[0] * 100;
            if (odd < cur)
            {
                index = 0;
                //Debug.Log(odd + " ----- " + cur);
            }
            else
                for (int i = 1; i < odds.Count; i++)
                {
                    cur += odds[i] * 100;
                    if (odd < cur)
                    {
                        index = i;
                        break;
                    }
                }
        }

        return index;
    }

    /// <summary>
    /// 
    /// </summary>
    static public T[] FindActive<T>() where T : Component
    {
        //return GameObject.FindSceneObjectsOfType(typeof(T)) as T[];
        return GameObject.FindObjectsOfType(typeof(T)) as T[];
    }

    /// <summary>
    ///
    /// </summary>
    static public T FindInParents<T>(GameObject go) where T : Component
    {
        if (go == null) return null;
        object comp = go.GetComponent<T>();

        if (comp == null)
        {
            Transform t = go.transform.parent;

            while (t != null && comp == null)
            {
                comp = t.gameObject.GetComponent<T>();
                t = t.parent;
            }
        }
        return (T)comp;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="go"></param>
    /// <param name="size"></param>
    /// <returns></returns>
    static public GameObject ChangeScaleInWorld(GameObject go, Vector3 size)
    {
        Transform parent = go.transform.parent;
        go.transform.parent = null;
        go.transform.localScale = size;
        go.transform.parent = parent;
        return go;
    }

    /// <summary> 
    /// 
    /// 
    /// </summary> 
    /// <param name="strText"></param> 
    /// <param name="encryptKey"></param> 
    /// <param name="encryptKey"></param> 
    public static string Encrypt(string inputString, string encryptKey)
    {
        byte[] byKey = null;
        byte[] IV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
        try
        {
            byKey = System.Text.Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            byte[] inputByteArray = Encoding.UTF8.GetBytes(inputString);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(byKey, IV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            return Convert.ToBase64String(ms.ToArray());
        }
        catch (System.Exception error)
        {
            Debug.Log(error);
            //return error.Message; 
            return null;
        }
    }

    /// <summary> 
    ///  
    /// </summary> 
    /// <param name="this.inputString"></param> 
    /// <param name="decryptKey"></param> 
    /// <param name="decryptKey"></param> 
    public static string Decrypt(string inputString, string decryptKey)
    {
        byte[] byKey = null;
        byte[] IV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xAB, 0xCD, 0xEF };
        byte[] inputByteArray = new Byte[inputString.Length];
        try
        {
            byKey = System.Text.Encoding.UTF8.GetBytes(decryptKey.Substring(0, 8));
            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
            inputByteArray = Convert.FromBase64String(inputString);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(byKey, IV), CryptoStreamMode.Write);
            cs.Write(inputByteArray, 0, inputByteArray.Length);
            cs.FlushFinalBlock();
            System.Text.Encoding encoding = new System.Text.UTF8Encoding();
            return encoding.GetString(ms.ToArray());
        }
        catch (System.Exception error)
        {
            Debug.Log(error);
            //return error.Message; 
            return null;
        }
    }

    /// <summary>
    /// 3D
    /// </summary>
    /// <param name="audio"></param>
    public static void SetAudioTo3D(AudioSource audio)
    {
        audio.playOnAwake = false;
        audio.minDistance = 0.5f;
        audio.maxDistance = 10f;
        audio.bypassEffects = true;
        audio.rolloffMode = AudioRolloffMode.Linear;
    }

    /// <summary>
    /// vector3.ToString() convert to vector3;
    /// </summary>
    /// <param name="rString"></param>
    /// <returns></returns>
    public static Vector3 StringToVector3(string rString)
    {
        string[] temp = rString.Substring(1, rString.Length - 2).Split(',');
        float x = float.Parse(temp[0]);
        float y = float.Parse(temp[1]);
        float z = float.Parse(temp[2]);
        Vector3 rValue = new Vector3(x, y, z);
        return rValue;
    }

    public static void DestroyChildren(Transform parent)
    {
        if (parent == null)
        {
            Debug.LogError("--------parent is null");
            return;
        }

        List<Transform> children = new List<Transform>();
        foreach (Transform t in parent)
        {
            children.Add(t);
        }

        for (int i = 0; i < children.Count; i++)
        {
            UnityEngine.Object.DestroyImmediate(children[i].gameObject);
        }
    }

    public static void AddMaterial(Renderer ren, Material mat)
    {
        if (ren == null || mat == null)
        {
            Debug.Log("mat or ren is null -----  ", ren);
            return;
        }
        List<Material> mats = new List<Material>(ren.materials);
        mats.Add(mat);
        ren.materials = mats.ToArray();
    }

    public static void RemoveLastMaterial(Renderer ren, bool keepOne)
    {
        if (ren == null) return;
        List<Material> mats = new List<Material>(ren.materials);
        if (keepOne && mats.Count < 2) return;

        if (mats.Count > 1)
        {
            mats.RemoveAt(mats.Count - 1);
            ren.materials = mats.ToArray();
        }
    }

    /// <summary>
    ///  (3D2D)，Z
    /// </summary>
    /// <param name="z">The z coordinate.</param>
    static public Vector3 ConvertPosByCam(Camera from, Camera to, Vector3 fromPos, float z = 0)
    {
        Vector3 viewPos = from.WorldToViewportPoint(fromPos);

        Vector3 toPos = to.ViewportToWorldPoint(viewPos);

        toPos.z = z;

        return toPos;
    }

    /// <summary>
    ///  (2D3D)，prePos3D
    /// </summary>
    /// <param name="prePos">Pre position.</param>
    static public Vector3 ConvertPosByCamKeepDis(Camera from, Camera to, Vector3 fromPos, Vector3 prePos)
    {
        float dis = GetDisForPointToTrans(prePos, to.transform);

        Vector3 camDis = from.transform.forward * dis;

        //
        Vector3 viewPos = from.WorldToViewportPoint(fromPos + camDis);

        //
        Vector3 worldPos = to.ViewportToWorldPoint(viewPos);

        return worldPos;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns>The dis for point to trans.</returns>
    static public float GetDisForPointToTrans(Vector3 pos, Transform trans)
    {
        //
        Vector3 line = pos - trans.position;
        float angle = Vector3.Angle(trans.forward, line);

        float disPosToTrans = Vector3.Distance(trans.position, pos);

        float cos = Mathf.Cos((Mathf.PI / 180) * angle);

        float minDis = cos * disPosToTrans;

        return minDis;
    }

    /// <summary>
    /// Convenience function that converts Class + Function combo into Class.Function representation.
    /// </summary>
    static public string GetFuncName(object obj, string method)
    {
        if (obj == null) return "<null>";
        string type = obj.GetType().ToString();
        int period = type.LastIndexOf('/');
        if (period > 0) type = type.Substring(period + 1);
        return string.IsNullOrEmpty(method) ? type : type + "/" + method;
    }

    static public string ConvertTimeToString(float second)
    {
        TimeSpan ts = new TimeSpan(0, 0, (int)second);
        return ts.ToString();
    }

    /// <summary>
    /// int，0,1(，)
    /// </summary>
    /// <param name="sum"></param>
    /// <param name="count"></param>
    static public void SubsectionInt(ref List<int> list, int sum, int count)
    {
        if (count == 1)
        {
            list.Add(sum);
            return;
        }
        else if (count < 1)
        {
            Debug.LogWarning("  0  " + sum + " List" + list.Count);
            return;
        }
        if (sum <= count)
        {
            if (sum < count)
                Debug.LogWarning("，" + sum + "----" + count + " List" + list.Count);
            for (int i = 0; i < count; i++)
                list.Add(1);
            return;
        }

        //
        int ranMax = sum - count + 1;

        if (ranMax < 2)
        {
            Debug.LogError("ranMax is error");
            return;
        }

        int sec = UnityEngine.Random.Range(1, ranMax);
        list.Add(sec);
        count--;
        sum -= sec;

        SubsectionInt(ref list, sum, count);
    }

    static public T StringToEnum<T>(string str)
    {
        return (T)Enum.Parse(typeof(T), str);
    }

    static public void AddRange<T>(List<T> per, List<T> addList)
    {
        if (per == null) per = new List<T>();
        for (int i = 0; i < addList.Count; i++)
        {
            per.Add(addList[i]);
        }
    }

    static public void AddRange<T>(List<T> per, T[] addList)
    {
        if (per == null) per = new List<T>();
        for (int i = 0; i < addList.Length; i++)
        {
            per.Add(addList[i]);
        }
    }
    // ，
    static List<int> randomNumPool = new List<int>();
    public static List<int> GetRandomNum(int max, int roll = -1)
    {
        randomNumPool.Clear();
        for (int i = 0; i < max; i++)
        {
            randomNumPool.Add(i);
        }
        UnityEngine.Random.InitState((int)Time.time);
        if (roll < 0)
        {
            roll = max / 2;
        }
        for (int j = 0; j < roll; j++)
        {
            int tmp1 = UnityEngine.Random.Range(0, max);
            int tmp2 = UnityEngine.Random.Range(0, max);
            int tmp = randomNumPool[tmp1];
            randomNumPool[tmp1] = randomNumPool[tmp2];
            randomNumPool[tmp2] = tmp;
        }
        return randomNumPool;
    }

    private static UnityEngine.Random.State seedGenerator;
    private static int seedGeneratorSeed = 1337;
    private static bool seedGeneratorInitialized = false;
    public static int GenerateSeed()
    {
        // remember old seed
        var temp = UnityEngine.Random.state;

        // initialize generator state if needed
        if (!seedGeneratorInitialized)
        {
            UnityEngine.Random.InitState(seedGeneratorSeed);
            seedGenerator = UnityEngine.Random.state;
            seedGeneratorInitialized = true;
        }

        // set our generator state to the seed generator
        UnityEngine.Random.state = seedGenerator;
        // generate our new seed
        var generatedSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        // remember the new generator state
        seedGenerator = UnityEngine.Random.state;
        // set the original state back so that normal random generation can continue where it left off
        UnityEngine.Random.state = temp;

        return generatedSeed;
    }

    /// <summary>
    /// （Fibonacci sequence）
    /// 1、1、2、3、5、8、13、21、34
    /// 3，。
    /// n = 1,2,3.....
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    static public double Fibonacci(int n)
    {
        double kaifan = Math.Pow(5, 0.5);
        return 1.0 / kaifan * (Math.Pow((1 + kaifan) / 2, n) - Math.Pow((1 - kaifan) / 2, n));
    }

    /// <summary>
    /// /// <summary>
    /// （Fibonacci sequence）
    /// Sn=A(n+1)+An-1=A(n+2)-1
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    static public double FibonacciSum(int n)
    {
        double sum = Fibonacci(n + 2) - 1;
        return sum;
    }

    // RenderTexture  
    static public string Capture(Camera camera, Rect rect, string fileName)
    {
        // RenderTexture    
        RenderTexture rt = new RenderTexture((int)rect.width, (int)rect.height, 0);
        // targetTexturert,     
        camera.targetTexture = rt;
        camera.Render();
        //ps: --- ，。    
        //ps: camera2.targetTexture = rt;    
        //ps: camera2.Render();    
        //ps: -------------------------------------------------------------------    

        // rt, 。    
        RenderTexture.active = rt;
        Texture2D screenShot = new Texture2D((int)rect.width, (int)rect.height, TextureFormat.RGB24, false);
        screenShot.ReadPixels(rect, 0, 0);// ：，RenderTexture.active    
        screenShot.Apply();

        // ，camera    
        camera.targetTexture = null;
        //ps: camera2.targetTexture = null;    
        RenderTexture.active = null; // JC: added to avoid errors    
        GameObject.Destroy(rt);

        // ，png    
        byte[] bytes = screenShot.EncodeToPNG();
        string path = Application.persistentDataPath + "/" + fileName + ".png";
        System.IO.File.WriteAllBytes(path, bytes);

        return path;
    }
}