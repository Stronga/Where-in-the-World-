/****************************************************************************** 
 * 
 * Maintaince Logs: 
 * 2017-02-16       WP      Initial version
 * 2020-10-27       WP      Rename and Update
 * 
 * *****************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using MzTool;

namespace MzDemo
{
    /// <summary>
    /// Scroll item 
    /// </summary>
    public partial class Demo_UIScrollItem : MzUGUIScrollItem
    {
        [SerializeField] protected Text textName = null;
        [SerializeField] protected Text textInfo = null;
        [SerializeField] protected Image imgIcon = null;
        [SerializeField] protected GameObject mScaleTarget = null;

        protected GameObject ScaleTarget
        {
            get
            {
                if (mScaleTarget != null) return mScaleTarget;
                return gameObject;
            }
        }
        

        private Demo_ParamsScrollItem paramsItem;
        

        public void Refresh(Demo_ParamsScrollItem config)
        {
            paramsItem = config;

            if(textName) textName.text = config.name;
            if(textInfo) textInfo.text = config.desc;
            if(imgIcon) imgIcon.sprite = config.sprite;

#if UNITY_EDITOR
            name = config.name;
#endif
        }

        public void Refresh(Demo_UIScrollItem other)
        {
            if(other)
            {
                Refresh(other.paramsItem);
            }
        }

        public override void SetScale(float size)
        {
            ScaleTarget.transform.localScale = new Vector3(size, size, size);
        }

        // Use this for initialization
        private void Start()
        {

        }
    }
}