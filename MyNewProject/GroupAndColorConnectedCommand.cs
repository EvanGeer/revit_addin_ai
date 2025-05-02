using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyNewProject
{
    public class GroupAndColorConnectedCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            Selection sel = uidoc.Selection;
            var ids = sel.GetElementIds();
            var selectedElements = ids.Select(id => doc.GetElement(id)).Where(e => e != null).ToList();

            // Build connectivity graph (using bounding box intersection)
            var groups = GroupConnectedElements(selectedElements);

            // Predefined colors (add more if needed)
            List<Color> colors = new List<Color>
            {
                new Color(255,0,0),   // Red
                new Color(0,255,0),   // Green
                new Color(0,0,255),   // Blue
                new Color(255,255,0), // Yellow
                new Color(255,0,255), // Magenta
                new Color(0,255,255), // Cyan
                new Color(128,0,128), // Purple
                new Color(255,128,0), // Orange
                new Color(0,128,255), // Light Blue
                new Color(128,128,128) // Gray
            };

            using (Transaction tx = new Transaction(doc, "Color Connected Groups"))
            {
                tx.Start();

                int colorIndex = 0;
                foreach (var group in groups)
                {
                    Color color = colors[colorIndex % colors.Count];
                    OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                    ogs.SetProjectionLineColor(color);
                    ogs.SetSurfaceForegroundPatternColor(color);
                    ogs.SetSurfaceForegroundPatternId(GetSolidFillPatternId(doc));

                    foreach (var elem in group)
                    {
                        doc.ActiveView.SetElementOverrides(elem.Id, ogs);
                    }
                    colorIndex++;
                }

                tx.Commit();
            }

            TaskDialog.Show("Groups", $"Found {groups.Count} connected groups.");

            return Result.Succeeded;
        }

        // Helper: Group connected elements using bounding box intersection
        private List<List<Element>> GroupConnectedElements(List<Element> elements)
        {
            var groups = new List<List<Element>>();
            var visited = new HashSet<ElementId>();

            foreach (var elem in elements)
            {
                if (visited.Contains(elem.Id))
                    continue;

                var group = new List<Element>();
                var queue = new Queue<Element>();
                queue.Enqueue(elem);
                visited.Add(elem.Id);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    group.Add(current);

                    var currentBox = current.get_BoundingBox(null);
                    if (currentBox == null) continue;

                    foreach (var other in elements)
                    {
                        if (visited.Contains(other.Id) || other.Id == current.Id)
                            continue;

                        var otherBox = other.get_BoundingBox(null);
                        if (otherBox == null) continue;

                        if (BoundingBoxesIntersect(currentBox, otherBox))
                        {
                            queue.Enqueue(other);
                            visited.Add(other.Id);
                        }
                    }
                }

                groups.Add(group);
            }

            return groups;
        }

        // Helper: Check if two bounding boxes intersect
        private bool BoundingBoxesIntersect(BoundingBoxXYZ a, BoundingBoxXYZ b)
        {
            return (a.Max.X >= b.Min.X && a.Min.X <= b.Max.X) &&
                   (a.Max.Y >= b.Min.Y && a.Min.Y <= b.Max.Y) &&
                   (a.Max.Z >= b.Min.Z && a.Min.Z <= b.Max.Z);
        }

        // Helper: Get the solid fill pattern id for coloring surfaces
        private ElementId GetSolidFillPatternId(Document doc)
        {
            return new FilteredElementCollector(doc)
                .OfClass(typeof(FillPatternElement))
                .Cast<FillPatternElement>()
                .FirstOrDefault(x => x.GetFillPattern().IsSolidFill)
                ?.Id ?? ElementId.InvalidElementId;
        }
    }
} 