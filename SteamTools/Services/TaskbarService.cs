﻿using Hardcodet.Wpf.TaskbarNotification;
using Livet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;

namespace SteamTools.Services
{
    public class TaskbarService : NotificationObject
    {
        #region static members

        public static TaskbarService Current { get; } = new TaskbarService();

        #region 托盘图标
        private TaskbarIcon _TaskBar;
        public TaskbarIcon Taskbar
        {
            get { return this._TaskBar; }
            set
            {
                if (this._TaskBar != value)
                {
                    this._TaskBar = value;
                    this.RaisePropertyChanged();
                }
            }
        }
        #endregion

        #endregion
        #region Message 変更通知
        private string notificationTitle;
        private string notificationMessage;
        /// <summary>
        /// 获取指示当前状态的字符串。
        /// </summary>
        public string Message
        {
            get { return this.notificationMessage; }
            set
            {
                this.notificationMessage = value;
                this.Taskbar.ShowBalloonTip(notificationTitle, this.notificationMessage, BalloonIcon.None);
                this.RaisePropertyChanged();
            }
        }

        #endregion

        public void Notify(string message, string title)
        {
            this.notificationTitle = title;
            this.Message = message;
        }

        public void Notify(string message)
        {
            this.Message = message;
        }

        private void RaiseMessagePropertyChanged()
        {
            this.RaisePropertyChanged(nameof(this.Message));
        }
    }
}
