﻿using AutumnBox.Basic.Device;
using AutumnBox.GUI.Model;
using AutumnBox.GUI.MVVM;
using AutumnBox.GUI.Properties;

using AutumnBox.Leafx.Container;
using AutumnBox.OpenFramework.Extension;
using AutumnBox.OpenFramework.Management;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using AutumnBox.OpenFramework.Management.ExtTask;
using AutumnBox.Logging;
using AutumnBox.GUI.Services;
using AutumnBox.Leafx.ObjectManagement;
using AutumnBox.OpenFramework.Management.ExtInfo;
using AutumnBox.OpenFramework.Exceptions;
using AutumnBox.GUI.Util;

namespace AutumnBox.GUI.ViewModel
{
    class VMExtensionsFinder : ViewModelBase
    {
        public IEnumerable<ExtensionWrapperDock> Docks
        {
            get => _docks; set
            {
                _docks = value;
                RaisePropertyChanged();
            }
        }
        private IEnumerable<ExtensionWrapperDock> _docks;

        public ExtensionWrapperDock SelectedDock
        {
            get => _selectedDock; set
            {
                _selectedDock = value;
                RaisePropertyChanged();
            }
        }
        private ExtensionWrapperDock _selectedDock;

        public ICommand ClickItem
        {
            get => _clickItem; set
            {
                _clickItem = value;
                RaisePropertyChanged();
            }
        }
        private ICommand _clickItem;

        public ICommand DoubleClickItem
        {
            get => _doubleClickItem; set
            {
                _doubleClickItem = value;
                RaisePropertyChanged();
            }
        }
        private ICommand _doubleClickItem;

        [AutoInject]
        private ILanguageManager LanguageManager { get; set; }

        [AutoInject]
        private readonly IAdbDevicesManager adbDevicesManager;

        [AutoInject]
        private readonly IOpenFxManager openFxManager;

        [AutoInject]
        private readonly IMessageBus messageBus;

        [AutoInject]
        private readonly INotificationManager notificationManager;

        public VMExtensionsFinder()
        {
            InitCommand();
            RaisePropertyChangedOnDispatcher = true;
            openFxManager.WakeIfLoaded(() =>
            {
                Load();
                messageBus.MessageReceived += (s, e) =>
                {
                    if (e.MessageType == Messages.REFRESH_EXTENSIONS_VIEW)
                    {
                        Load();
                    }
                };
                LanguageManager.LanguageChanged += (s, e) => Load();
                adbDevicesManager.DeviceSelectionChanged += (s, e) => Order();
            });
        }

        private void Load()
        {
            var libsManager = OpenFx.Lake.Get<ILibsManager>();
            Docks = libsManager.Wrappers()
                    .Region(LanguageManager.Current.LanCode)
                    .Hide()
                    .Dev(Settings.Default.DeveloperMode)
                    .ToDocks();
            SLogger<VMExtensionsFinder>.Info(Docks.Count());
            Order();
        }

        private void Order()
        {
            Docks = from dock in Docks
                    orderby dock.Wrapper.Info[ExtensionMetadataKeys.PRIORITY] descending
                    orderby dock.Execute.CanExecuteProp descending
                    select dock;
        }

        private void InitCommand()
        {
            ClickItem = new FlexiableCommand((p) =>
            {
                if (!Settings.Default.DoubleClickRunExt)
                {
                    StartExtension((p as ExtensionWrapperDock)?.Wrapper);
                }
            });
            DoubleClickItem = new FlexiableCommand((p) =>
            {
                if (Settings.Default.DoubleClickRunExt)
                {
                    StartExtension((p as ExtensionWrapperDock)?.Wrapper);
                }
            });
        }

        private void StartExtension(IExtensionInfo inf)
        {
            try
            {
                this.GetComponent<IExtensionTaskManager>().Start(inf);
            }
            catch (DeviceStateIsNotCorrectException)
            {
                notificationManager.Warn("IS NOT TARGET STATE ERROR");
            }
        }
    }
}
