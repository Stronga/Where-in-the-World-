/****************************************************************************** 
 * 
 * Maintaince Logs: 
 * 2020-10-27       WP      Initial version
 * 
 * *****************************************************************************/
 
using UnityEngine;
using UnityEngine.EventSystems;

namespace MzTool
{
    public partial class MzUGUIScrollItem : MonoBehaviour , IPointerClickHandler
    {
        public Vector3 worldPos { get { return transform.position; } }

        public delegate void DelOnClick(MzUGUIScrollItem item);
        protected DelOnClick eventOnClick;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventOnClick != null) eventOnClick(this);
        }
        
        /// <summary>
        /// Call when state change to Current
        /// </summary>
        public virtual void SetToCurrent()
        {

        }
        
        /// <summary>
        /// Call when the state changes from Current to non-Current
        /// </summary>
        public virtual void SetToNonCurrent()
        {

        }
        
        /// <summary>
        ///  Call when state change to Select
        /// </summary>
        public virtual void SetToSelect()
        {

        }
        
        /// <summary>
        /// Call when the state changes from selected to non-selected
        /// </summary>
        public virtual void SetToUnselect()
        {

        }
        
        /// <summary>
        /// Call when the item is centered 
        /// </summary>
        /// <param name="size"></param>
        public virtual void SetScale(float size)
        {
            transform.localScale = new Vector3(size, size, size);
        }

        public void LoginButtonEvent(DelOnClick method)
        {
            eventOnClick = method;
        }
    }
}