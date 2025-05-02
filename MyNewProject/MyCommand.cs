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
            using (Transaction tx = new Transaction(doc, "Add Spheres"))
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

                    // Create a sphere solid at the location
                    double radius = 0.5; // 1' diameter
                    Solid sphere = CreateSphere(location, radius);

                    if (sphere != null)
                    {
                        // Create a DirectShape and add the sphere geometry
                        DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                        ds.SetShape(new List<GeometryObject> { sphere });
                        ds.Name = "SelectionSphere";
                    }
                }

                tx.Commit();
            }

            TaskDialog.Show("Selected ElementIds", concatenatedIds);

            return Result.Succeeded;
        }

        // Helper to create a sphere solid at a given location
        private Solid CreateSphere(XYZ center, double radius)
        {
            // Create a semicircle in the XZ plane
            Arc arc = Arc.Create(
                new XYZ(center.X, center.Y, center.Z - radius),
                new XYZ(center.X, center.Y, center.Z + radius),
                new XYZ(center.X + radius, center.Y, center.Z)
            );

            CurveLoop profile = new CurveLoop();
            profile.Append(arc);

            // Axis of revolution: vertical through the center
            Line axis = Line.CreateBound(
                new XYZ(center.X, center.Y, center.Z - radius - 1),
                new XYZ(center.X, center.Y, center.Z + radius + 1)
            );

            try
            {
                // This signature is available in Revit 2022+
                return GeometryCreationUtilities.CreateRevolvedGeometry(
                    new List<CurveLoop> { profile }, 0, 2 * Math.PI, axis
                );
            }
            catch
            {
                return null;
            }
        }
    }
} 