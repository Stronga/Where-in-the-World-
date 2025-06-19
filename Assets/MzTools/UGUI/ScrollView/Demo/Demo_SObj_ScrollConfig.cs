/****************************************************************************** 
 * 
 * Maintaince Logs: 
 * 2020-10-27       WP      Initial version
 * 
 * *****************************************************************************/
 
using UnityEngine;

namespace MzDemo
{

    [System.Serializable]
    public struct Demo_ParamsScrollItem
    {
        public string name;
        public Sprite sprite;
        public string desc;
    }

    [CreateAssetMenu(fileName = "Demo_SObj_ScrollConfig", menuName = "MzDemo/Demo_SObj_ScrollConfig", order = 100)]
    public class Demo_SObj_ScrollConfig : ScriptableObject
    {
        public Demo_ParamsScrollItem[] arrayItemInfo;
    }
}