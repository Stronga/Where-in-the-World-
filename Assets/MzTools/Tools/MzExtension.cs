/****************************************************************************** 
 * 
 * Maintaince Logs: 
 * 2018-06-23     WP      Initial version
 * 
 * *****************************************************************************/

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Extension 
/// </summary>
static public class MzExtension
{
    static public T AddChild<T>(this Transform transform, T prefab, bool isChangeLayer = true, bool isPreSize = false) where T : Component
    {
        return MzTools.AddChild(transform.gameObject, prefab, isChangeLayer, isPreSize);
    }

    static public T AddChild<T>(this GameObject gameObject, T prefab, bool isChangeLayer = true, bool isPreSize = false) where T : Component
    {
        return MzTools.AddChild(gameObject, prefab, isChangeLayer, isPreSize);
    }

    static public GameObject AddGameObject(this GameObject gameObject, GameObject prb, bool isChangeLayer = true, bool isPreSize = false)
    {
        return MzTools.AddGameObj(gameObject, prb, isChangeLayer, isPreSize);
    }

    static public T Clone<T>(this T prefab, bool isChangeLayer = true, bool isPrefabSize = true) where T : MonoBehaviour
    {
        return MzTools.AddChildByClone(prefab, isChangeLayer, isPrefabSize);
    }

    static public GameObject Clone(this GameObject g, bool isChangeLayer = true, bool isPreSize = true)
    {
        var go = AddGameObject(g.transform.parent.gameObject, g, isChangeLayer, isPreSize);

        go.transform.localPosition = g.transform.localPosition;
        go.transform.localRotation = g.transform.localRotation;

        return go;
    }

    static public void SetEmit(this ParticleSystem particleSystem, bool isEmit)
    {
        var emit = particleSystem.emission;
        emit.enabled = isEmit;
    }

    /// <summary>
    /// set Active
    /// </summary>
    /// <param name="mono">Mono.</param>
    /// <param name="active">If set to <c>true</c> active.</param>
    static public void SetActive(this MonoBehaviour mono, bool active)
    {
        var go = mono.gameObject;
        go.SetActiveSafe(active);
    }

    static public void SetActiveSafe(this GameObject go, bool isActive)
    {
        if (go.activeSelf != isActive)
            go.SetActive(isActive);
    }

    static public string AddlineToText(this string text)
    {
        text += System.Environment.NewLine;
        return text;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    static public string FirstLetterToUpper(this string str)
    {
        if (str == null)
            return null;

        if (str.Length > 1)
            return char.ToUpper(str[0]) + str.Substring(1);

        return str.ToUpper();
    }

    static public int ToInt(this string str)
    {
        int value = 0;
        if (int.TryParse(str, out value)) { }
        else Debug.LogError("parse int error : " + str);
        return value;
    }

    static public Vector3 ToVec3(this Vector2 v2)
    {
        return new Vector3(v2.x, v2.y, 0);
    }
    
    /// <summary>
    /// get type in list
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="list"></param>
    /// <returns></returns>
    public static System.Type GetTypeInList<T>(this List<T> list)
    {
        return typeof(T);
    }

    public static void AddSafe<T,U>(this Dictionary<T, U> dict, T t, U u)
    {
        if(dict == null) dict = new Dictionary<T, U>();

        if(dict.ContainsKey(t))
            dict[t] = u;
        else
            dict.Add(t, u);
    }
}