/****************************************************************************** 
 * 
 * Maintaince Logs: 
 * 2020-10-27       WP      Initial version
 * 
 * *****************************************************************************/
 
using System.Collections.Generic;
using UnityEngine;
using MzTool;
using UnityEngine.UI;

namespace MzDemo
{

    public class Demo_UIScrollCtrl : MonoBehaviour
    {
        private List<Demo_UIScrollItem> items = new List<Demo_UIScrollItem>();
        [SerializeField] private Demo_SObj_ScrollConfig config = null;
        [SerializeField] private MzUGUIScrollCtrl scrollCtrl = null;
        [SerializeField] private Demo_UIScrollItem templateItem = null;
        [SerializeField] private Text textState = null;

        private Demo_UIScrollItem curSelectItem;

        // Start is called before the first frame update
        private void Start()
        {
            if (config != null && scrollCtrl)
            {
                var configItems = config.arrayItemInfo;

                var uiItems = scrollCtrl.CreateItems(configItems.Length);

                for (int i = 0; i < uiItems.Count; i++)
                {
                    var uiItem = uiItems[i] as Demo_UIScrollItem;
                    uiItem.Refresh(configItems[i]);

                    items.Add(uiItem);
                }

                scrollCtrl.onSelectItemChange.AddListener(OnTemplateItem);
            }
        }

        private void OnTemplateItem(MzUGUIScrollItem item)
        {
            curSelectItem = item as Demo_UIScrollItem;

            if(templateItem) templateItem.Refresh(curSelectItem);

            RefreshStateText();
        }

        public void OnClickSelect()
        {
            scrollCtrl.SetSelectItemToCurrentItem();

            RefreshStateText();
        }

        private void RefreshStateText()
        {
            if(textState)
            {
                textState.text = scrollCtrl.IsChosenWithSelectItem ? "Current" : "Choose";
            }
        }
    }
}