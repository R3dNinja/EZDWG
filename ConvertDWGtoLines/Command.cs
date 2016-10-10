#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace ConvertDWGtoLines
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        internal static Command thisCommand = null;
        public ProgressForm p_MyForm;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            thisCommand = this;
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            try
            {
                //App.thisApp.ShowForm(commandData.Application);
                ShowForm(commandData.Application);
                ConvertDWGtoDetailLines(uidoc);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            //convert DWG lines to Detail Lines using Kirksey's "*Solid  (02-Thin)"
            //ConvertDWGtoDetailLines(uidoc);
            //return Result.Succeeded;
        }

        public void ShowForm(UIApplication uiapp)
        {
            if (p_MyForm == null || p_MyForm.IsDisposed)
            {
                p_MyForm = new ProgressForm(uiapp);
                p_MyForm.Show();
            }
        }

        public void ConvertDWGtoDetailLines(UIDocument uidoc)
        {
            Document doc = uidoc.Document;
            ImportInstance currentDWG = SelectDWG(uidoc);
            if (currentDWG != null)
            {
                int counter = ConvertDWG(doc, currentDWG);
                if (counter > 0)
                {
                    String stringToShow = "Converted " + counter.ToString() + " elements to detail lines.";
                    p_MyForm.EndOfConversion(stringToShow);
                }
                else
                {

                    TaskDialog.Show("Error", "Please select a DWG import or link.");
                    p_MyForm.EndOfConversion("Error");
                }
            }
        }

        private ImportInstance SelectDWG(UIDocument uidoc)
        {
            Document doc = uidoc.Document;
            Reference reference = uidoc.Selection.PickObject(ObjectType.Element);
            Element el = uidoc.Document.GetElement(reference);

            if (el.GetType().FullName == "Autodesk.Revit.DB.ImportInstance")
            {
                ImportInstance curLink = el as ImportInstance;
                return curLink;
            }
            else
            {
                return null;
            }
        }

        private int ConvertDWG(Document doc, ImportInstance currentDWG)
        {
            string docType;
            if (doc.IsFamilyDocument == true)
            {
                docType = "family";
            }
            else
            {
                docType = "project";
            }


            string lineStyleToUse;
            int counter = 0;

            List<GeometryObject> curGeometryList = GetLinkedDWGCurves(currentDWG);

            int maxValue = curGeometryList.Count;
            int runningValue = 0;
            p_MyForm.SetupProgress(maxValue, "Converting DWG to Detail Lines");
            
            //Check and set linestyletouse
            if (doesLinestyleExist(doc, "*Solid  (02-Thin)") == true)
            {
                lineStyleToUse = "*Solid  (02-Thin)";
            }
            else
            {
                lineStyleToUse = "Thin Lines";
            }

            using (Transaction tx = new Transaction(doc))
            {
                tx.Start("Convert DWG to Lines");
                if (curGeometryList.Count != 0)
                {
                    //create detail lines for elements in geometry list
                    foreach (GeometryObject curGeom in curGeometryList)
                    {
                        if (curGeom.GetType().FullName == "Autodesk.Revit.DB.PolyLine")
                        {
                            //Since Revit can't handle polylines
                            PolyLine curPolyLine = curGeom as PolyLine;
                            IList<XYZ> ptsList = curPolyLine.GetCoordinates();
                            for (int i = 0; i <= ptsList.Count - 2; i++)
                            {
                                try
                                {
                                    if (docType == "project")
                                    {
                                        //project file
                                        DetailCurve newLine = doc.Create.NewDetailCurve(doc.ActiveView, Line.CreateBound(ptsList[i], ptsList[i + 1]));
                                        newLine.LineStyle = getLinestyleByName(doc, lineStyleToUse) as Element;
                                    }
                                    else
                                    {
                                        //family file
                                        try
                                        {
                                            DetailCurve newLine = doc.FamilyCreate.NewDetailCurve(doc.ActiveView, Line.CreateBound(ptsList[i], ptsList[i + 1]));
                                            newLine.LineStyle = getLinestyleByName(doc, lineStyleToUse) as Element;
                                        }
                                        catch
                                        {
                                            TaskDialog.Show("Error", " Cannot create detail line in this type of family.");
                                            return 0;
                                        }
                                    }
                                }
                                catch
                                {
                                    Debug.Print("Could not create polyline");
                                }
                                counter = counter + 1;
                            }
                        }
                        else
                        {
                            //create a line in the current view
                            try
                            {
                                if (docType == "project")
                                {
                                    DetailCurve newLine = doc.Create.NewDetailCurve(doc.ActiveView, curGeom as Curve);
                                    newLine.LineStyle = getLinestyleByName(doc, lineStyleToUse) as Element;
                                }
                                else
                                {
                                    try
                                    {
                                        DetailCurve newLine = doc.FamilyCreate.NewDetailCurve(doc.ActiveView, curGeom as Curve);
                                        newLine.LineStyle = getLinestyleByName(doc, lineStyleToUse) as Element;
                                    }
                                    catch
                                    {
                                        TaskDialog.Show("Error", " Cannot create detail line in this type of family.");
                                        return 0;
                                    }
                                }
                                counter = counter + 1;
                            }
                            catch
                            {
                                Debug.Print("Could not create polyline");
                            }
                        }
                        runningValue++;
                        if (runningValue < maxValue)
                        {
                            p_MyForm.IncrementProgress();
                        }
                    }
                }
                tx.Commit();
            }
            return counter;
        }

        private List<GeometryObject> GetLinkedDWGCurves(ImportInstance currentDWG)
        {
            List<GeometryObject> curvelist = new List<GeometryObject>();
            Options curOptions = new Options();
            GeometryElement geoElement;
            GeometryElement geoElement2;
            GeometryInstance geoInstance;

            geoElement = currentDWG.get_Geometry(curOptions);

            foreach (GeometryObject geoObject in geoElement)
            {
                //convert geoObject to geoInstance
                geoInstance = geoObject as GeometryInstance;
                geoElement2 = geoInstance.GetInstanceGeometry();

                foreach (GeometryObject curObject in geoElement2)
                {
                    curvelist.Add(curObject);
                }
            }
            return curvelist;
        }

        private bool doesLinestyleExist(Document doc, String linestyleName)
        {
            try
            {
                getLinestyleByName(doc, linestyleName);
                return true;
            }
            catch
            {
                return false;
            }

        }

        private GraphicsStyle getLinestyleByName(Document doc, String linestyleName)
        {
            Category curCat = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Lines).SubCategories.get_Item(linestyleName);
            return curCat.GetGraphicsStyle(GraphicsStyleType.Projection);
        }
    }
}
