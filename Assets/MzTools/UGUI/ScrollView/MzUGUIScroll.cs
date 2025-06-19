/****************************************************************************** 
 * 
 * Maintaince Logs: 
 * 2017-02-20       WP      Initial version
 * 2020-10-27       WP      Rename and Update
 * 
 * *****************************************************************************/

using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MzTool
{
    [RequireComponent(typeof(ScrollRect))]
    /// <summary>
    /// 用来判断是否还在拖动列表
    /// </summary>
    public partial class MzUGUIScroll : MonoBehaviour , IBeginDragHandler, IEndDragHandler
    {
        public bool isDraging { private set; get; }

        internal MzUGUIScrollCtrl ctrl;

        public virtual void OnBeginDrag(PointerEventData eventData)
        {
            isDraging = true;
        }

        public virtual void OnEndDrag(PointerEventData eventData)
        {
            isDraging = false;
            
            if(ctrl) ctrl.StickWithDragEnd();
        }
    }
}