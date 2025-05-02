using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Linq;
using System.Collections.Generic;
using Autodesk.Revit.Creation;
using System;

namespace MyNewProject
{
    public class MyCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Autodesk.Revit.DB.Document doc = uidoc.Document;
            Selection sel = uidoc.Selection;
            var ids = sel.GetElementIds();

            // Concatenate all ElementIds as a comma-separated string
            string concatenatedIds = string.Join(",", ids.Select(id => id.IntegerValue.ToString()));

            // Start a transaction to create DirectShapes
            using (Transaction tx = new Transaction(doc, "Add Vertical Lines"))
            {
                tx.Start();

                foreach (var id in ids)
                {
                    Element elem = doc.GetElement(id);
                    XYZ location = null;

                    // Try to get the element's location
                    if (elem.Location is LocationPoint lp)
                    {
                        location = lp.Point;
                    }
                    else if (elem.Location is LocationCurve lc)
                    {
                        location = lc.Curve.GetEndPoint(0); // Use start point of curve
                    }
                    else
                    {
                        // Skip elements without a usable location
                        continue;
                    }

                    // Create a vertical line 1' tall at the location
                    Line verticalLine = CreateVerticalLine(location, 1.0);

                    if (verticalLine != null)
                    {
                        // Create a DirectShape and add the line geometry
                        DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                        ds.SetShape(new List<GeometryObject> { verticalLine });
                        ds.Name = "SelectionLine";
                    }
                }

                tx.Commit();
            }

            TaskDialog.Show("Selected ElementIds", concatenatedIds);

            return Result.Succeeded;
        }

        // Helper to create a vertical line 1' tall at a given location
        private Line CreateVerticalLine(XYZ basePoint, double height)
        {
            XYZ topPoint = new XYZ(basePoint.X, basePoint.Y, basePoint.Z + height);
            return Line.CreateBound(basePoint, topPoint);
        }
    }
} 