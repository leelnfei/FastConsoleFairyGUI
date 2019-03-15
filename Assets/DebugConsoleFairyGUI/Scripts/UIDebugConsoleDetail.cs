﻿/*
 * @Author: fasthro
 * @Date: 2019-02-25 15:12:01
 * @Description: UIDebugConsoleDetail 日志条目详情界面
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FairyGUI;

namespace DebugConsoleFairyGUI
{
    public class UIDebugConsoleDetail : Window
    {
        // DebugConsole 实例
        private DebugConsole manager;
        private LogEntry m_logEntry;
        #region component
        private GButton m_copyBtn;
        private GButton m_closeBtn;

        private GList m_list;
        private GComponent m_listItem;

        private Controller m_typeController;
        
        #endregion

        public UIDebugConsoleDetail(DebugConsole mgr)
        {
            manager = mgr;
        }

        protected override void OnInit()
        {
            contentPane = UIPackage.CreateObject(LogConst.UI_PACKAGE_NAME, "detail_panel").asCom;
            contentPane.MakeFullScreen();
            sortingOrder = manager.sortingOrder;
            gameObjectName = "Window - DebugConsoleDetail";
        }

        protected override void OnShown()
        {
            m_copyBtn = contentPane.GetChild("copy_btn").asButton;
            m_copyBtn.onClick.Set(OckCopy);

            m_closeBtn = contentPane.GetChild("close_btn").asButton;
            m_closeBtn.onClick.Set(OckClose);

            m_list = contentPane.GetChild("list").asList;
            m_list.RemoveChildrenToPool();
            m_listItem = m_list.GetFromPool("").asCom;
            m_list.AddChild(m_listItem);

            m_typeController = contentPane.GetController("type");
        }

        public void Refresh(LogEntry logEntry)
        {
            m_logEntry = logEntry;
           if (logEntry.logType == DebugLogType.Log)
            {
                m_typeController.SetSelectedIndex(0);
            }
            else if (logEntry.logType == DebugLogType.Warning)
            {
                m_typeController.SetSelectedIndex(1);
            }
            else if (logEntry.logType == DebugLogType.Error)
            {
                m_typeController.SetSelectedIndex(2);
            }

            var content_text = m_listItem.GetChild("content_text").asRichTextField;
            content_text.text = logEntry.ToString();

            var textHeight = content_text.textHeight;
            var maxHeight = m_list.height;

            if (textHeight > maxHeight)
            {
                m_listItem.height = textHeight;
                m_list.touchable = true;
            }
            else
            {
                m_list.touchable = false;
                m_listItem.height = maxHeight;
            }


            m_list.scrollPane.ScrollTop();
        }

        private void OckCopy()
        {
            Debug.Log("copy -> " + m_logEntry.ToString());
            DebugConsole.Copy(m_logEntry.ToString());
        }

        private void OckClose()
        {
            Hide();
        }
    }
}

