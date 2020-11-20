﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Windows;
using System.Reflection;
using System.Security.Principal;
using MetroRadiance.UI;
using MetroTrilithon.Lifetime;
using System.Runtime.CompilerServices;
using Livet;
using SteamTool.Proxy;
using System.Threading;
using System.IO;
using MetroTrilithon.Desktop;
using SteamTool.Core.Common;
using SteamTools.Services;
using MetroTrilithon.Mvvm;
using Hardcodet.Wpf.TaskbarNotification;
using SteamTools.Models;

namespace SteamTools
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : INotifyPropertyChanged, IDisposableHolder
    {
        public static App Instance => Current as App;

        private void IsRenameProgram()
        {
            string strFullPath = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
            if ($"{ProductInfo.Title}.exe" != strFullPath)
            {
                MessageBox.Show("禁止修改程序名称", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Shutdown();
            }
        }

        /// <summary>
        /// 启动时
        /// </summary>
        /// <param name="e"></param>
        protected override void OnStartup(StartupEventArgs e)
        {
#if !DEBUG
            var appInstance = new MetroTrilithon.Desktop.ApplicationInstance().AddTo(this);
            if (appInstance.IsFirst)
#endif
            {
                IsRenameProgram();

#if DEBUG
                if (e.Args.Length != 0)
                {
                    this.ProcessCommandLineParameter(e.Args);
                    base.OnStartup(e);
                    return;
                }
#endif

                Logger.EnableTextLog = true;
                this.DispatcherUnhandledException += App_DispatcherUnhandledException;
                DispatcherHelper.UIDispatcher = this.Dispatcher;

                //托盘加载
                TaskbarService.Current.Taskbar = (TaskbarIcon)FindResource("Taskbar");
                ThemeService.Current.Register(this, Theme.Windows, Accent.Windows);
                WindowService.Current.AddTo(this).Initialize();
                ProxyService.Current.Initialize();
                SteamConnectService.Current.Initialize();

                this.MainWindow = WindowService.Current.GetMainWindow();
                this.MainWindow.Show();

#if !DEBUG
                appInstance.CommandLineArgsReceived += (sender, args) =>
                {
                    // 检测到多次启动时将主窗口置于最前面
                    this.Dispatcher.Invoke(() => WindowService.Current.MainWindow.Activate());
                    this.ProcessCommandLineParameter(args.CommandLineArgs);
                };
#endif
                base.OnStartup(e);
            }
#if !DEBUG
            else
            {
                appInstance.SendCommandLineArgs(e.Args);
            }
#endif
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            try
            {
                Logger.Error($"{Assembly.GetExecutingAssembly().GetName().Name} Run Error : {Environment.NewLine}", e.Exception);
                MessageBox.Show(e.Exception.ToString(), "发生错误");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            Current.Shutdown();
        }

        /// <summary>
        /// 程序退出
        /// </summary>
        /// <param name="e"></param>
        protected override void OnExit(ExitEventArgs e)
        {
            if (TaskbarService.Current.Taskbar != null)
            {
                //TaskbarService.Current.Taskbar.Icon = null; //避免托盘图标没有自动消失
                TaskbarService.Current.Taskbar.Icon.Dispose();
            }
            if (ProxyService.Current.Proxy != null)
            {
                ProxyService.Current.Proxy.Dispose();
            }
            foreach (var app in SteamConnectService.Current.RuningSteamApps)
            {
                if (!app.Process.HasExited)
                    app.Process.Kill();
            }
            base.OnExit(e);
        }

        private void ProcessCommandLineParameter(string[] args)
        {
            Debug.WriteLine("多重启动通知: " + args.ToString(" "));
            // 当使用命令行参数多次启动时，您可以执行某些操作
            IsRenameProgram();
            if (args.Length == 0)
                this.Shutdown();
            if (!int.TryParse(args[0], out var appId))
                this.Shutdown();

            Logger.EnableTextLog = true;
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
            DispatcherHelper.UIDispatcher = this.Dispatcher;

            ThemeService.Current.Register(this, Theme.Windows, Accent.Windows);
            WindowService.Current.AddTo(this).Initialize(appId);
            //SteamConnectService.Current.Initialize();

            this.MainWindow = WindowService.Current.GetMainWindow();
            this.MainWindow.Show();

        }

        #region INotifyPropertyChanged members

        private event PropertyChangedEventHandler PropertyChangedInternal;
        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { this.PropertyChangedInternal += value; }
            remove { this.PropertyChangedInternal -= value; }
        }

        private void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChangedInternal?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable members
        private readonly LivetCompositeDisposable compositeDisposable = new LivetCompositeDisposable();
        ICollection<IDisposable> IDisposableHolder.CompositeDisposable => this.compositeDisposable;

        void IDisposable.Dispose()
        {
            GC.SuppressFinalize(this);
            this.compositeDisposable.Dispose();
        }

        #endregion
    }
}
