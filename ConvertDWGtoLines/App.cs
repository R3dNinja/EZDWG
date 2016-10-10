#region Namespaces
using System;
using System.Collections.Generic;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
#endregion

namespace ConvertDWGtoLines
{
    class App : IExternalApplication
    {
        internal static App thisApp = null;
        public ProgressForm p_MyForm;

        public Result OnStartup(UIControlledApplication a)
        {
            p_MyForm = null;
            thisApp = this;
            //CreateRibbonTab cTab = new CreateRibbonTab();
           // cTab.tabAndButtons(a);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication a)
        {
            if (p_MyForm != null && p_MyForm.Visible)
            {
                p_MyForm.Close();
            }
            return Result.Succeeded;
        }

        public void ShowForm(UIApplication uiapp)
        {
            if (p_MyForm == null || p_MyForm.IsDisposed)
            {
                p_MyForm = new ProgressForm(uiapp);
                p_MyForm.Show();
            }
        }
    }
}
