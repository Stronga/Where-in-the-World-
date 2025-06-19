/****************************************************************************** 
 * 
 * Maintaince Logs: 
 * 2017-02-14       WP      Initial version
 * 2020-10-27       WP      Rename and Update
 * 
 * *****************************************************************************/

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;

namespace MzTool
{
    /// <summary>
    /// 用于角色的UI展示、解锁、选择等入口
    /// 由于UGUI Scroll Rect （ScrollView） 的一此限制，需要手动的设置中一些参数，来达到最佳的体验效果
    /// 也必须遵守一些规则，来适应此脚本
    /// 1、Scroll View 和 View Point 默认中心点，如果设置transCenter则代表这个是中心点。
    /// 2、Content物品上加入了两个脚本：Horizontal(Vertical) Layer Group 和 Content Size Fitter 两个
    ///     前者是用来自动对齐物品的坐标，并且预留滑动条两侧的距离（如果没有这个距离，最边上的两个物品将无法在中间显示）
    ///     后者是用来自动适应Content的大小，这对动态加载物品很有帮助
    /// </summary>
    public partial class MzUGUIScrollCtrl : MonoBehaviour
    {
        [SerializeField, Header("Config"), MethodButton("E_RefreshScrollRect", "Refresh")] private ScrollRect scrollRect = null;
        [SerializeField, Tooltip("This object is under \"Content\" and will be used during dynamic creation"), MethodButton("E_RefreshItem1", "Refresh")] private MzUGUIScrollItem item01 = null;
        [SerializeField, Tooltip("If it is null, it will center on the origin of the ScrollRect")] private Transform transCenter = null;
        /// <summary>
        /// Load the elements under the Content object
        /// </summary>
        [SerializeField] private bool loadOnStart = false;

        /// <summary>
        /// Whether to save the last selection locally
        /// </summary>
        [SerializeField, Header("Record")] private bool recordCurrentItem = false;
        /// <summary>
        /// Stick to current object when enabled
        /// </summary>
        [SerializeField] private bool isStickCurrentOnEnable = true;
        /// <summary>
        /// Key saved locally
        /// </summary>
        [SerializeField] private string recordKey = "Last_Choosed_Index";

        /// <summary>
        /// stick time and velocity curve
        /// </summary>
        [Header("Effect Params")]
        [SerializeField] private AnimationCurve stickAnimCurve = AnimationCurve.EaseInOut(0, 0, 0.25f, 1);
        [SerializeField, Tooltip("When the scrolling speed is less than this value, it will be stick"), Range(0.0001f, 0.005f)] private float minMoveDelta = 0.002f;

        /// <summary>
        /// stick when move speed Greater than this value
        /// </summary>
        [SerializeField] [Range(0.001f, 1)] private float minStickSpeed = 0.01f;
        /// <summary>
        /// Minimum stick distance, stick when !isRebounding and distance Greater than this value
        /// </summary>
        [SerializeField] [Range(0.1f, 2)] private float minStickDistance = 1f;
        /// <summary>
        /// Amplification effect switch
        /// </summary>
        [SerializeField] private bool isCenterScaleEffect = true;
        /// <summary>
        /// For LocalScale the default is 1
        /// </summary>
        [SerializeField] [Range(1, 5)] private float itemEffectSize = 1.4f;
        /// <summary>
        /// At the end of the manual, if the list speed is less than this value, it will be directly stick
        /// </summary>
        [SerializeField] [Range(0.5f, 5)] private float stickOnEndDragSpeed = 1.5f;
        /// <summary>
        /// Range of effect: The unit is meters, not pixels
        /// </summary>
        [SerializeField] [Range(0.1f, 1000)] private float scaleRange = 200;

        [System.Serializable]
        public class OnItemChange : UnityEvent<MzUGUIScrollItem> { }

        /// <summary>
        /// The event when this item is selected
        /// </summary>
        [Header("Events")]
        public OnItemChange onSelectItemChange;
        /// <summary>
        /// Event when the current item changes
        /// </summary>
        public OnItemChange onCurrentItemChange;

        private MzUGUIScroll mScroll;
        private MzUGUIScroll scroll
        {
            get
            {
                if (mScroll == null && scrollRect)
                {
                    mScroll = scrollRect.gameObject.AddComponent<MzUGUIScroll>();
                }
                return mScroll;
            }
        }

        private RectTransform parentContent
        {
            get
            {
                if (scrollRect == null)
                {
                    Debug.LogError("scroll is null");
                    return transform as RectTransform;
                }
                return scrollRect.content;
            }
        }

        /// <summary>
        /// 当前所展示的物品
        /// </summary>
        private MzUGUIScrollItem curShowItem
        {
            get { return mSelectItem; }
            set
            {
                if (mSelectItem != value)
                {
                    if (mSelectItem != null) mSelectItem.SetToUnselect();

                    mSelectItem = value;
                    mSelectItem.SetToSelect();

                    if (onSelectItemChange != null) onSelectItemChange.Invoke(mSelectItem);
                }
            }
        }

        private MzUGUIScrollItem currentItem
        {
            get { return mCurrentItem; }
            set
            {
                if (mCurrentItem != value)
                {
                    if (mCurrentItem != null) mCurrentItem.SetToNonCurrent();

                    mCurrentItem = value;
                    mCurrentItem.SetToCurrent();

                    if (recordCurrentItem) IndexOfLast = items.IndexOf(value);

                    if (onCurrentItemChange != null) onCurrentItemChange.Invoke(mCurrentItem);
                }
            }
        }

        private int IndexOfLast
        {
            get
            {
                return PlayerPrefs.GetInt(recordKey, 0);
            }
            set
            {
                PlayerPrefs.SetInt(recordKey, value);
            }
        }

        private bool IsHorizontal
        {
            get
            {
                if (scrollRect) return scrollRect.horizontal;
                return false;
            }
        }

        private bool IsVertical
        {
            get
            {
                if (scrollRect) return scrollRect.vertical;
                return false;
            }
        }

        public bool IsChosenWithSelectItem { get { return currentItem == curShowItem; } }

        [SerializeField, HideInInspector, Header("Debug")] private bool isSticking;
        [SerializeField, HideInInspector] private float moveDelta;
        [SerializeField, HideInInspector] private float lastEndDragSpeed = 0;
        [SerializeField, HideInInspector] private Vector2 curScrollPos = Vector2.zero;
        [SerializeField, HideInInspector] private Vector2 preScrollPos = Vector2.zero;
        [SerializeField, HideInInspector] private MzUGUIScrollItem mSelectItem = null;
        [SerializeField, HideInInspector] private MzUGUIScrollItem mCurrentItem = null;
        [SerializeField, HideInInspector] private Vector3 fromPos = Vector3.zero;
        [SerializeField, HideInInspector] private Vector3 toPos = Vector3.zero;
        [SerializeField, HideInInspector] private float keyParam = 0;

        private float fromPosX { get { return fromPos.x; } }
        private float toPosX { get { return toPos.x; } }
        private float fromPosY { get { return fromPos.y; } }
        private float toPosY { get { return toPos.y; } }
        private Vector3 centerRelativePos
        {
            get
            {
                if (transCenter) return GetRelativePos(transCenter);
                else if (scroll) return GetRelativePos(scroll.transform);
                return Vector3.zero;
            }
        }
        private Vector3 centerWorldPos
        {
            get
            {
                if (transCenter) return transCenter.position;
                else if (scroll) return scroll.transform.position;
                return Vector3.zero;
            }
        }
        private bool isRebounding
        {
            get
            {
                return curScrollPos.x < 0 || curScrollPos.x > 1 || curScrollPos.y < 0 || curScrollPos.y > 1;
            }
        }

        private List<MzUGUIScrollItem> items = new List<MzUGUIScrollItem>();


        // Use this for initialization
        private void Start()
        {
            if (scroll) scroll.ctrl = this;
            else
            {
                enabled = false;
                Debug.LogError("scrollRect is null!");
                return;
            }

            scrollRect.onValueChanged.AddListener(OnScroll);

            if (loadOnStart) LoadItems();
        }

        private void OnEnable()
        {
            if (isStickCurrentOnEnable) LoadCurrentItem();
        }

        private void LoadCurrentItem()
        {

            if (recordCurrentItem)
            {
                var index = IndexOfLast;
                if (index > -1 && index < items.Count)
                {
                    mCurrentItem = items[index];
                    StartCoroutine(DoLoadCurrentItem());
                }
            }
        }

        private IEnumerator DoLoadCurrentItem()
        {
            yield return null;
            SetAndStickItem(mCurrentItem);
        }

        private void OnScroll(Vector2 pos)
        {
            if (!enabled) return;

            if (isCenterScaleEffect)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    MzUGUIScrollItem item = items[i];

                    float dis = 0;
                    if (IsHorizontal && IsVertical) dis = Mathf.Abs(Vector2.Distance(centerWorldPos, item.worldPos));
                    else if (IsHorizontal) dis = Mathf.Abs(centerWorldPos.x - item.worldPos.x);
                    else dis = Mathf.Abs(centerWorldPos.y - item.worldPos.y);

                    float size = Mathf.Lerp(1, itemEffectSize, 1 - dis / scaleRange);
                    item.SetScale(size);
                }
            }
            curScrollPos = pos;

            //move speed 
            if (!isSticking && !scroll.isDraging)
            {
                moveDelta = Vector2.Distance(preScrollPos, pos);
                preScrollPos = pos;

                if (moveDelta < minMoveDelta)
                {
                    Stick();
                }
            }
        }

        /// <summary>
        /// 吸附
        /// </summary>
        protected virtual void Stick()
        {
            float minDis = 1000;
            MzUGUIScrollItem item = null;
            for (int i = 0; i < items.Count; i++)
            {
                float curDis = Vector3.Distance(items[i].worldPos, centerWorldPos);
                if (curDis < minDis)
                {
                    minDis = curDis;
                    item = items[i];
                }
            }
            if (item != null)
            {
                SetAndStickItem(item);
            }
        }

        private void StickToSelectItem()
        {
            keyParam = 0;

            RefreshMovePos();

            var movePos = Vector3.zero;
            if (IsHorizontal) movePos.x = toPosX - fromPosX;
            if (IsVertical) movePos.y = toPosY - fromPosY;
            if (movePos.magnitude < minStickSpeed)
            {
                parentContent.Translate(movePos);
                scrollRect.StopMovement();
            }
            else
            {
                if (isRebounding && movePos.magnitude < minStickDistance)
                {

                }
                else StartCoroutine(StickToPos());
            }
        }

        private IEnumerator StickToPos()
        {
            scrollRect.StopMovement();

            isSticking = true;
            //当前Curve 的 value 如果超过了1或者低于0，有相应数对应
            yield return new WaitForEndOfFrame();
            int length = stickAnimCurve.length;
            float time = stickAnimCurve[length - 1].time;

            RefreshMovePos();

            while (keyParam < time)
            {
                keyParam += Time.unscaledDeltaTime;
                if (keyParam > time) keyParam = time;
                float factor = stickAnimCurve.Evaluate(keyParam);

                var lerpPos = Vector3.Lerp(fromPos, toPos, factor);
                SetContentPos(lerpPos);
                yield return new WaitForEndOfFrame();
            }

            //force move
            SetContentPos(toPos);
            scrollRect.StopMovement();

            yield return new WaitForEndOfFrame();
            isSticking = false;
        }

        private void OnClickItem(MzUGUIScrollItem item)
        {
            SetAndStickItem(item);
        }

        private Vector3 GetRelativePos(Transform t)
        {
            return transform.InverseTransformPoint(t.position);
        }

        private void RefreshMovePos()
        {
            fromPos = parentContent.position;
            //这里应当使用局部（相对）坐标，否则在旋转加持下，Content的坐标Z会发生变化 
            toPos = GetRelativePos(parentContent) + centerRelativePos - GetRelativePos(curShowItem.transform);
            toPos = transform.TransformPoint(toPos);
        }

        private void SetContentPos(Vector3 worldPos)
        {
            var localPos = parentContent.localPosition;
            parentContent.position = worldPos;
            if(!IsHorizontal) localPos.y = parentContent.localPosition.y;
            if(!IsVertical) localPos.x = parentContent.localPosition.x;
            parentContent.localPosition = localPos;
        }

        #region public Methods

        /// <summary>
        /// Use item01 as a template to dynamically create and return the entire array
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<MzUGUIScrollItem> CreateItems(int count)
        {
            if (item01 == null || count < 1)
            {
                Debug.LogError("Prefab is null!");
                return null;
            }

            items.Add(item01);
            for (int i = 1; i < count; i++)
            {
                var item = item01.Clone();
                item.LoginButtonEvent(OnClickItem);
                items.Add(item);
            }
            item01.LoginButtonEvent(OnClickItem);

            LoadCurrentItem();

            return items;
        }

        /// <summary>
        /// Statically read all the elements under the Content object and return the entire array
        /// </summary>
        /// <returns></returns>
        public List<MzUGUIScrollItem> LoadItems()
        {
            if (parentContent == null)
                return null;

            for (int i = 0; i < parentContent.childCount; i++)
            {
                var t = parentContent.GetChild(i);
                MzUGUIScrollItem si = t.GetComponent<MzUGUIScrollItem>();
                if (si == null) si = t.gameObject.AddComponent<MzUGUIScrollItem>();

                si.LoginButtonEvent(OnClickItem);
                items.Add(si);
            }

            LoadCurrentItem();

            return items;
        }

        /// <summary>
        /// Get all current element array
        /// </summary>
        /// <returns></returns>
        public List<MzUGUIScrollItem> GetItems() { return items; }

        /// <summary>
        /// Select and stick to the element
        /// </summary>
        /// <param name="item"></param>
        public void SetAndStickItem(MzUGUIScrollItem item)
        {
            if (isSticking) return;
            curShowItem = item;
            StickToSelectItem();
        }

        /// <summary>
        /// Set Selected Item State to "Current use"
        /// </summary>
        public void SetCurrentItemToSelectItem()
        {
            currentItem = curShowItem;
        }

        /// <summary>
        /// Set Select Item to Current Item
        /// </summary>
        public void SetSelectItemToCurrentItem()
        {
            if (currentItem) currentItem = curShowItem;
        }

        /// <summary>
        /// Button event:Set Seletc Item of the next index
        /// </summary>
        public void BtnNextItem()
        {
            int index = items.IndexOf(curShowItem);

            if (index + 1 < items.Count)
            {
                SetAndStickItem(items[index + 1]);
            }
        }

        /// <summary>
        /// Button event: Set Select Item of the previous index
        /// </summary>
        public void BtnPrevItem()
        {
            int index = items.IndexOf(curShowItem);

            if (index > 0)
            {
                SetAndStickItem(items[index - 1]);
            }
        }

        #endregion

        internal virtual void StickWithDragEnd()
        {
            lastEndDragSpeed = scrollRect.velocity.magnitude;
            if (lastEndDragSpeed < stickOnEndDragSpeed && !isSticking)
            {
                Stick();
            }
        }


#if UNITY_EDITOR

        private void E_RefreshScrollRect()
        {
            if (scrollRect == null) scrollRect = GetComponentInChildren<ScrollRect>();

            if (scrollRect && scrollRect.viewport && scrollRect.content)
            {
                var group = parentContent.GetComponent<LayoutGroup>();
                Vector2 itemSize = Vector2.zero;
                if (group)
                {
                    var size = scrollRect.viewport.rect.size;
                    if (parentContent.childCount > 0)
                    {
                        var rtItem = parentContent.GetChild(0) as RectTransform;
                        if (rtItem)
                        {
                            itemSize = rtItem.rect.size;
                        }
                    }

                    if (IsHorizontal)
                    {
                        int min = (int)(size.x / 2 - itemSize.x / 2 + 4);
                        group.padding.left = min;
                        group.padding.right = min;
                    }

                    if (IsVertical)
                    {
                        int min = (int)(size.y / 2 - itemSize.x / 2 + 4);
                        group.padding.top = min;
                        group.padding.bottom = min;
                    }

                    group.SetActive(false);
                    group.SetActive(true);
                }
            }
        }

        private void E_RefreshItem1()
        {
            if (scrollRect && scrollRect.content)
            {
                if (parentContent.childCount > 0)
                {
                    item01 = parentContent.GetChild(0).GetComponent<MzUGUIScrollItem>();
                }
            }
        }

#endif
    }
}