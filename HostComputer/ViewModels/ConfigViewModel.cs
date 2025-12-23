using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ComponentEntities;
using HostComputer.Common.Base;
using HostComputer.Common.Services.Components;
using HostComputer.Common.Services.LocalDataService.Component;
using HostComputer.Models;

namespace HostComputer.ViewModels
{
    public class ConfigViewModel : ViewModelBase
    {
        private readonly ComponentLocalService _localService;
        private Random random = new();
        private string _sourceViewName;

        public ObservableCollection<DeviceItemModel> DeviceList { get; set; } = new();
        public List<ThumbModel> ThumbList { get; set; }
        public ICommand DropCommand { get; set; }
        public ICommand ThumbCommand { get; set; }
        public ICommand SaveCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        // --- 核心改进：当外部传入 SourceViewName 后，自动触发加载 ---
        public string SourceViewName
        {
            get => _sourceViewName;
            set
            {
                _sourceViewName = value;
                // 只要值一传进来，立刻去读文件
                LoadLayoutFromFile();
            }
        }

        public ConfigViewModel()
        {
            _localService = new ComponentLocalService();

            // 初始化左侧工具栏数据
            InitThumbList();

            // 保存命令
            SaveCommand = new CommandBase
            {
                DoExecute = _ =>
                {
                    SaveLayoutToFile();
                    CloseWindow(true);
                }
            };

            // 取消命令
            CancelCommand = new CommandBase { DoExecute = _ => CloseWindow(false) };

            DropCommand = new CommandBase { Name = "DropCommand", DoExecute = DoDropCommand };
            ThumbCommand = new CommandBase { Name = "ThumbCommand", DoExecute = DoThumbCommand };
        }

        private void LoadLayoutFromFile()
        {
            if (string.IsNullOrEmpty(SourceViewName))
                return;

            // 这里传入外部传进来的 SourceViewName (如 "PageA")
            var config = _localService.LoadLayout(SourceViewName);
            if (config != null)
            {
                DeviceList.Clear();
                foreach (var device in config.Devices)
                {
                    DeviceList.Add(device);
                }
                // 如果本地文件里存的名字和传入的不一致，以传入的为准（或覆盖）
            }
        }

        private void SaveLayoutToFile()
        {
            var config = new LayoutConfig
            {
                SourceViewName = this.SourceViewName,
                Devices = DeviceList.ToList()
            };

            bool result = _localService.SaveLayout(config, SourceViewName);

            if (result)
            {
                MessageBox.Show("保存成功！", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("保存失败！", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseWindow(bool dialogResult)
        {
            // 通过当前 DataContext 找到宿主窗口并关闭
            var window = Application
                .Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.DataContext == this);
            if (window != null)
            {
                window.DialogResult = dialogResult;
                window.Close();
            }
        }

        private void DoDropCommand(object? obj)
        {
            if (obj is DragEventArgs e)
            {
                var data = (ThumbItemModel)e.Data.GetData(typeof(ThumbItemModel));
                var element = e.Source as IInputElement;
                if (element == null)
                    return;

                var point = e.GetPosition(element);
                DeviceList.Add(
                    new DeviceItemModel
                    {
                        DeviceNum = random.Next(1, 99).ToString(),
                        X = point.X - data.Width / 2,
                        Y = point.Y - data.Height / 2,
                        Width = data.Width,
                        Height = data.Height,
                        DeviceType = data.TargetType
                    }
                );
            }
        }

        private void DoThumbCommand(object obj)
        {
            if (obj is Border border && border.DataContext is ThumbItemModel item)
            {
                DragDrop.DoDragDrop(border, item, DragDropEffects.Copy);
            }
        }

        private void InitThumbList()
        {
            ThumbList = new List<ThumbModel>();

            ThumbModel thumb = new ThumbModel()
            {
                Header = "设备",

                Children = new List<ThumbItemModel>()
                {
                    new ThumbItemModel()
                    {
                        TargetType = "WaferRobot",

                        Width = 200,

                        Height = 200,

                        Icon = "&#xe669;"
                    },
                    new ThumbItemModel()
                    {
                        TargetType = "AirCompressor",

                        Width = 200,

                        Height = 200,

                        Icon = "&#xe6a3;"
                    },
                    new ThumbItemModel(),
                    new ThumbItemModel(),
                    new ThumbItemModel(),
                }
            };

            ThumbList.Add(thumb);

            thumb = new ThumbModel()
            {
                Header = "控制开关",

                Children = new List<ThumbItemModel>()
                {
                    new ThumbItemModel(),
                    new ThumbItemModel(),
                    new ThumbItemModel(),
                    new ThumbItemModel(),
                    new ThumbItemModel()
                }
            };

            ThumbList.Add(thumb);

            thumb = new ThumbModel()
            {
                Header = "管道",

                Children = new List<ThumbItemModel>()
                {
                    new ThumbItemModel(),
                    new ThumbItemModel(),
                    new ThumbItemModel(),
                    new ThumbItemModel(),
                    new ThumbItemModel()
                }
            };

            ThumbList.Add(thumb);

            thumb = new ThumbModel()
            {
                Header = "数字仪表",

                Children = new List<ThumbItemModel>()
                {
                    new ThumbItemModel(),
                    new ThumbItemModel(),
                    new ThumbItemModel(),
                    new ThumbItemModel(),
                    new ThumbItemModel()
                }
            };

            ThumbList.Add(thumb);
            AddSampleThumbs();
        }

        private void AddSampleThumbs()
        {
            // 填充你的设备、管道、仪表等...
            // ... 保持你原始代码逻辑即可
        }
    }
}
