#region Namespace
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace ConvertDWGtoLines
{
    class DWGConverter
    {

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
                    App.thisApp.p_MyForm.EndOfConversion(stringToShow);
                }
                else
                {
                    TaskDialog.Show("Error", "Please select a DWG import or link.");
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
                                    //Debug.Print("Could not create polyline");
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
                                //Debug.Print("Could not create polyline");
                            }
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
