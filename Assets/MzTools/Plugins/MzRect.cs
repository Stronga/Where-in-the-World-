/****************************************************************************** 
 * 
 * Maintaince Logs: 
 * 2016-03-30     WP      Initial version
 * 
 * *****************************************************************************/

using UnityEngine;
using System.Collections.Generic;

namespace MzTool
{

    /// <summary>
    /// 提供于Rect的便用方法
    /// </summary>
    public static class MzRect
    {
        /// <summary>
        /// Hierarchy 里面从右往左数的位置
        /// </summary>
        static public Rect H_CR(this Rect rect, int index = 1, float offectX = 0)
        {
            int dis = 2 * index;
            rect.x += rect.width - index * rect.height - dis - offectX;

            return H_Size(rect);
        }

        /// <summary>
        /// 重置定义Rect的大小
        /// </summary>
        static public Rect H_Size(this Rect rect, float width = 16, float height = 16)
        {
            rect.size = new Vector2(width, height);
            return rect;
        }

        /// <summary>
        /// 设置左边到指定宽度大小，并out 右边
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="width"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        static public Rect H_WidthLeft(this Rect rect, float width, out Rect right)
        {
            right = new Rect(rect);
            right.x += width;

            right.width -= width;

            rect.width = width;

            return rect;
        }

        static public Rect H_WidthRight(this Rect rect, float width, out Rect left)
        {
            left = rect.H_WidthLeft(rect.width - width, out rect);
            return rect;
        }

        /// <summary>
        /// Hs the division.
        /// 按百分比对一个Rect进行水平方向上的分割
        /// </summary>
        /// <param name="rect">Rect.</param>
        /// <param name="percentLeft">Percent left. 0 到 1 之间的浮点数</param>
        static public void H_Division(this Rect rect, float percentLeft, out Rect left, out Rect right)
        {
            float width = rect.width;

            float wLeft = width * percentLeft;
            float wRight = width - wLeft;
            var pos = rect.position;
            left = new Rect(pos, new Vector2(wLeft, rect.height));
            pos.x += wLeft;
            right = new Rect(pos, new Vector2(wRight, rect.height));
        }

        /// <summary>
        /// 按平均数为水平方向分割
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="count">列数</param>
        /// <param name="rects"></param>
        static public void H_Division(this Rect rect, int count, out List<Rect> rects)
        {
            float height = rect.height;
            rects = new List<Rect>();
            var pos = rect.position;

            if (count > 0)
            {
                float width = rect.width / count;
                for (int i = 0; i < count; i++)
                {
                    Rect r = new Rect(pos, new Vector2(width, height));
                    rects.Add(r);
                    pos.x += width;

                }
            }
        }
    }
}