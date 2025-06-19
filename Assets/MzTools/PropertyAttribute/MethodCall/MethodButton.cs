/****************************************************************************** 
 * 
 * Maintaince Logs: 
 * 2018-07-08     WP      Initial version
 * 
 * *****************************************************************************/

//using System;

namespace MzTool
{
    /// <summary>
    /// Inspector 上画方法的按钮
    /// </summary>
    public class MethodButton : UnityEngine.PropertyAttribute
    {
        public string[] methods;
        public string[] showNames;
        public float buttonWidth = 80;

        public delegate void Action();

        public MethodButton(string method, float buttonWidth = 80)
        {
            methods = new string[1] { method };
            showNames = new string[1] { method };
            this.buttonWidth = buttonWidth;
        }

        public MethodButton(string method, string name, float buttonWidth = 80)
        {
            methods = new string[1] { method };
            showNames = new string[1] { name };
            this.buttonWidth = buttonWidth;
        }

        public MethodButton(string method, string name, string method2, string name2, float buttonWidth = 60)
        {
            methods = new string[2] { method, method2 };
            showNames = new string[2] { name, name2 };
            this.buttonWidth = buttonWidth;
        }
    }
}