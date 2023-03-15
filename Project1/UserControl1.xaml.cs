using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TankPlugin
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UI : Window
    {
        public UI()
        {
            InitializeComponent();
        }

        private void BtnCreateTank_Click(object sender, RoutedEventArgs e)
        {
            Project1.View_Model.TankUIVM TankVM = (Project1.View_Model.TankUIVM)DataContext;

            TankVM.FillTankData(TankVM.ObjTank);
           if( TankVM.ObjTank.DrawTank())
           {
               
           }
        }
    }
}
