using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Runtime;
using Autodesk.Windows;
using Project1.Support2D;

namespace TankPlugin
{
    public class RibbonCreation
    {
        public void AddRibbonTab(RibbonControl ribbonControl)
        {
            try
            {
                RibbonTab ribbonTab = ribbonControl.FindTab("Plant Utilities");
                if (ribbonTab != null)
                    ribbonControl.Tabs.Remove(ribbonTab);

                ribbonTab = new RibbonTab
                {
                    Title = "Plant Utilities",
                    Id = "Plant Utilities"
                };

                //Add the Tab
                ComponentManager.Ribbon.Tabs.Add(ribbonTab);
                this.AddTabContent(ribbonTab);

                ribbonTab.IsActive = true;
            }
            catch (System.Exception)
            {
               
            }
        }

        public void AddTabContent(RibbonTab ribbonTab)
        {
            try
            {
                ribbonTab.Panels.Add(this.AddImportFilePanel());
            }
            catch (System.Exception)
            {
                throw;
            }
        }
        /// <summary>
        /// Add panel for ACAD ribbon
        /// </summary>
        /// <returns></returns>
        private Autodesk.Windows.RibbonPanel AddImportFilePanel()
        {
            var ribbonPanelSource = new RibbonPanelSource
            {
                Title = "Plant Utilities"
            };
            var ribbonPanel = new Autodesk.Windows.RibbonPanel
            {
                Source = ribbonPanelSource
            };

            var bomButton = CreateRibonButton("Create Support 2D", "Create \nSupport 2D", true, true, RibbonItemSize.Large, System.Windows.Controls.Orientation.Vertical, "billcon.png");

            if (bomButton != null)
            {
                bomButton.CommandHandler = new Support2d();
                ribbonPanelSource.Items.Add(bomButton);
            }


            return ribbonPanel;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="buttonName"></param>
        /// <param name="buttonText"></param>
        /// <param name="showText"></param>
        /// <param name="showImage"></param>
        /// <param name="size"></param>
        /// <param name="orientation"></param>
        /// <param name="icon"></param>
        /// <returns></returns>
        public RibbonButton CreateRibonButton(string buttonName, string buttonText, bool showText, bool showImage, RibbonItemSize size, System.Windows.Controls.Orientation orientation, string icon)
        {
            var ribbonButton = new RibbonButton
            {
                Name = buttonName,
                ShowText = showText,
                Text = buttonText,
                ShowImage = showImage,
                Orientation = orientation,
                Size = size
            };

            return ribbonButton;
        }
    }

    public class Support2d : System.Windows.Input.ICommand
    {
        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;

        [Obsolete]
        public void Execute(object parameter)
        {
            SupportC SupportCentral = new SupportC();
            SupportCentral.ReadSupportData();
            SupportCentral.ProcessSupportData();
            SupportCentral.Create2D();
        }
    }
}
