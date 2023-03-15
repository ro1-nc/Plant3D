using Project1.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Project1.View_Model
{
    class TankUIVM : INotifyPropertyChanged
    {
        public double  _TankRadius = 0.0;
        public double TankRadius
        {
            get
            {
                return _TankRadius;
            }
            set
            {
                _TankRadius = value;
                RaisePropertyChanged(nameof(TankRadius));
            }
        }

        public double _TankHeight = 0.0;
        public double TankHeight
        {
            get
            {
                return _TankHeight;
            }
            set
            {
                _TankHeight = value;
                RaisePropertyChanged(nameof(TankHeight));
            }
        }

        public double _TankThickness = 0.0;
        public double TankThickness
        {
            get
            {
                return _TankThickness;
            }
            set
            {
                _TankThickness = value;
                RaisePropertyChanged(nameof(TankThickness));
            }
        }

        public Tank ObjTank = new Tank();

        public void FillTankData(Tank ObjsTank)
        {
            ObjsTank.Height = TankHeight;
            ObjsTank.Radius = TankRadius;
            ObjsTank.Thickness = TankThickness;
        }

        #region EventHandler
        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
