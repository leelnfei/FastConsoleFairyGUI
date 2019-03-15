﻿/*
 * @Author: fasthro
 * @Date: 2019-02-18 15:15:36
 * @Description: DebugConsoleFairyGUI 管理类 
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FairyGUI;
using System.Text.RegularExpressions;
using System;

namespace DebugConsoleFairyGUI
{
    public enum DebugLogType
    {
        Log,
        Warning,
        Error,
    }

    public class DebugConsole : MonoBehaviour
    {
        private static DebugConsole instance = null;

        // UI渲染层级
        public int sortingOrder = int.MaxValue;
        // android 包名
        public string packageName = "com.fasthro.debugconsolefairygui";

        // 主界面
        [HideInInspector]
        public UIDebugConsole mainUI;

        // 设置配置
        [HideInInspector]
        public DebugConsoleSetting settingConfig;

        // Native
        [HideInInspector]
        private Native m_native;

        // 命令
        private Command m_command;

        #region 列表数据

        // log 条目列表
        private List<LogEntry> logEntrys;

        // 合并字典<日志条目,在logShowEntrys列表中的索引>
        private Dictionary<LogEntry, int> sameEntryDic;

        // 合并所有条目字典<日志条目,在logShowEntrys列表中的索引>
        private Dictionary<LogEntry, bool> sameEntryAllDic;

        // 过滤字符串
        private string m_filter;

        // UI当前显示的列表
        [HideInInspector]
        public List<LogEntry> logShowEntrys;

        // 日志数量
        [HideInInspector]
        public int infoCount;
        [HideInInspector]
        public int warnCount;
        [HideInInspector]
        public int errorCount;
        #endregion

        #region 状态
        // 收缩
        private bool m_collapsed;
        public bool collapsed
        {
            get
            {
                return m_collapsed;
            }
        }

        //info
        private bool m_infoSelected = true;
        public bool infoSelected
        {
            get
            {
                return m_infoSelected;
            }
        }

        // warn
        private bool m_warnSelected = true;
        public bool warnSelected
        {
            get
            {
                return warnSelected;
            }
        }

        // error
        private bool m_errorSelected = true;
        public bool errorSelected
        {
            get
            {
                return errorSelected;
            }
        }

        #endregion

        void OnEnable()
        {
            Application.logMessageReceived -= ReceivedLog;
            Application.logMessageReceived += ReceivedLog;

            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(this);

                // 添加UI包
                UIPackage.AddPackage(LogConst.UI_PACKAGE_PATH);

                // log 条目列表
                logEntrys = new List<LogEntry>(128);
                logShowEntrys = new List<LogEntry>(128);
                sameEntryDic = new Dictionary<LogEntry, int>(128);
                sameEntryAllDic = new Dictionary<LogEntry, bool>(128);

                // native
                m_native = new Native();
                m_native.InitializeAndroid(packageName);

                // 命令
                m_command = new Command();
            }
        }

        private void OnDisable()
        {
            Application.logMessageReceived -= ReceivedLog;

            if(m_native != null){
                m_native.Release();
                m_native = null;
            }
        }

        private void Start()
        {
            // 初始化配置
            settingConfig = Resources.Load("DebugConsoleSetting") as DebugConsoleSetting;
            // 列表字体颜色设置
            settingConfig.listFontColor = DebugConsoleSetting.LIST_FONT_COLOR[settingConfig.listFontColorIndex];

            // 创建主UI
            mainUI = new UIDebugConsole(this);
            mainUI.Show();
        }

        private void ReceivedLog(string logString, string stackTrace, LogType logType)
        {
            LogEntry logEntry = new LogEntry(logString, stackTrace, ULTToDLT(logType));

            logEntrys.Add(logEntry);

            SetReceivedLog(logEntry);

            mainUI.Refresh();
        }

        // 设置log
        private void SetReceivedLog(LogEntry logEntry)
        {
            if (!m_collapsed)
            {
                if (FilterRule(logEntry))
                {
                    logShowEntrys.Add(logEntry);
                }

                SetCount(logEntry);
            }
            else
            {
                int index = -1;
                if (sameEntryDic.TryGetValue(logEntry, out index))
                {
                    logShowEntrys[index].logCount++;
                }
                else
                {
                    if (FilterRule(logEntry))
                    {
                        sameEntryDic.Add(logEntry, logShowEntrys.Count);

                        logShowEntrys.Add(logEntry);
                    }
                }

                if (!sameEntryAllDic.ContainsKey(logEntry))
                {
                    sameEntryAllDic.Add(logEntry, true);

                    SetCount(logEntry);
                }
            }
        }

        // Unity LogType to DebugLogType
        private DebugLogType ULTToDLT(LogType logType)
        {
            if (logType == LogType.Log)
                return DebugLogType.Log;
            else if (logType == LogType.Warning)
                return DebugLogType.Warning;
            return DebugLogType.Error;
        }

        // 日志过滤规则
        private bool FilterRule(LogEntry entry)
        {
            if ((m_infoSelected && entry.logType == DebugLogType.Log)
            || (m_warnSelected && entry.logType == DebugLogType.Warning)
            || (m_errorSelected && entry.logType == DebugLogType.Error))
            {
                if (!string.IsNullOrEmpty(m_filter))
                {
                    Regex rgx = new Regex(m_filter);
                    Match match = rgx.Match(entry.logContent);
                    return match.Success;
                }
                return true;
            }
            return false;
        }

        // 设置日志数量
        private void SetCount(LogEntry logEntry)
        {
            if (logEntry.logType == DebugLogType.Log)
            {
                infoCount++;
            }
            else if (logEntry.logType == DebugLogType.Warning)
            {
                warnCount++;
            }
            else if (logEntry.logType == DebugLogType.Error)
            {
                errorCount++;
            }
        }

        // 重置
        private void Reset()
        {
            infoCount = 0;
            warnCount = 0;
            errorCount = 0;

            logShowEntrys.Clear();
            sameEntryDic.Clear();
            sameEntryAllDic.Clear();

            for (int i = 0; i < logEntrys.Count; i++)
            {
                logEntrys[i].Reset();
                SetReceivedLog(logEntrys[i]);
            }

            mainUI.Refresh();
        }

        // 清理Log
        public void ClearLog()
        {
            logEntrys.Clear();
            Reset();
            mainUI.Refresh();
        }

        // 设置收缩
        public void SetCollapsed(bool selected)
        {
            this.m_collapsed = selected;

            Reset();
        }

        // 设置Info
        public void SetInfo(bool selected)
        {
            this.m_infoSelected = selected;

            Reset();
        }

        // 设置warn
        public void SetWarn(bool selected)
        {
            this.m_warnSelected = selected;

            Reset();
        }

        // 设置error
        public void SetError(bool selected)
        {
            this.m_errorSelected = selected;

            Reset();
        }

        // 设置过滤内容
        public void SetFilter(string filter)
        {
            this.m_filter = filter;

            Reset();
        }

        #region 日志输出函数
        public static void Log(object message)
        {
            Debug.Log(message);
        }

        public static void LogWarning(object message)
        {
            Debug.LogWarning(message);
        }

        public static void LogError(object message)
        {
            Debug.LogError(message);
        }

        #endregion

        #region native
        // copy
        public static void Copy(string content)
        {
            if(instance != null)
            {
                instance.m_native.Copy(content);
            }
        }

        #endregion

        #region 命令
        // 执行命令
        public static void ExecuteCommand(string cmd){
            if(instance != null)
            {
                instance.m_command.Execute(cmd);
            }
        }

        // 添加静态命令
        public static void AddStaticCommand(string cmd, string description, string methodName, Type type)
        {
            if(instance != null)
            {
                instance.m_command.AddCommand(cmd, description, methodName, type, null);
            }
        }

        // 添加对象命令
        public static void AddCommand(string cmd, string description, string methodName, Type type, object inst)
        {
            if(instance != null)
            {
                instance.m_command.AddCommand(cmd, description, methodName, type, inst);
            }
        }
        #endregion
    }
}


