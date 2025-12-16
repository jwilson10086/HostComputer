using HostComputer.Common.Base;
using HostComputer.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace HostComputer.Models
{
    public class DeviceItemModel : ObservableObject
    {
        public Func<List<DeviceItemModel>> Devices { get; set; }

        public string DeviceNum { get; set; }
        public string Header { get; set; }

        private bool _isSelected;

        int z_temp = 0;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                Set(ref _isSelected, value);

                if (value)
                {
                    z_temp = this.Z;
                    this.Z = 999;
                }
                else this.Z = z_temp;
            }
        }

        private bool _isVisible = true;

        public bool IsVisible
        {
            get { return _isVisible; }
            set { Set(ref _isVisible, value); }
        }


        private double x;
        public double X
        {
            get { return x; }
            set { Set(ref x, value); }
        }
        private double y;
        public double Y
        {
            get { return y; }
            set { Set(ref y, value); }
        }
        private int z = 0;

        public int Z
        {
            get { return z; }
            set { Set(ref z, value); }
        }

        private double _width;
        public double Width
        {
            get { return _width; }
            set { Set(ref _width, value); }
        }
        private double _height;
        public double Height
        {
            get { return _height; }
            set { Set(ref _height, value); }
        }

        private int _rotate;

        public int Rotate
        {
            get { return _rotate; }
            set { Set(ref _rotate, value); }
        }

        private int _flowDirection;

        public int FlowDirection
        {
            get { return _flowDirection; }
            set { Set(ref _flowDirection, value); }
        }



        // 根据这个名称动态创建一个组件实例
        public string DeviceType { get; set; }


        public DeviceItemModel()
        {

        }
      
    }
}
