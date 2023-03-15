using Project1.View_Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TankPlugin
{
    public class UIBackend
    {
        UI Dlg = new UI();
        TankUIVM TankVM = new TankUIVM();
        public void Show()
        {
            Dlg = new UI()
            {
                DataContext = TankVM
            };
            
            Dlg.ShowDialog();
        }
    }
}
