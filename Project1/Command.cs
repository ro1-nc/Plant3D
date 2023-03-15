using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;

[assembly: CommandClass(typeof(TankPlugin.TankCommand))]
namespace TankPlugin
{
    public class TankCommand
    {
        [CommandMethod("LoadABC", CommandFlags.Transparent)]
        public void RibbonPanelCreation()
        {
            var componentManager = ComponentManager.Ribbon;
            var ribbonCreation = new RibbonCreation();
            ribbonCreation.AddRibbonTab(componentManager);
        }

    }
}
