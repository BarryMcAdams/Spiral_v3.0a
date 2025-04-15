using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Linq;
using System.Windows.Forms;
using acadApp = Autodesk.AutoCAD.ApplicationServices.Application;

namespace SpiralStaircasePlugin
{
    public class StaircaseCalculator
    {
        // Add private fields for parameters
        private double _centerPoleDiameter;
        private double _overallHeight;
        private double _outsideDia;
        private double _rotationDeg;

        // Constructor
        public StaircaseCalculator(double centerPoleDiameter, double overallHeight, double outsideDia, double rotationDeg)
        {
            _centerPoleDiameter = centerPoleDiameter;
            _overallHeight = overallHeight;
            _outsideDia = outsideDia;
            _rotationDeg = rotationDeg;
        }

    // Removed extra brace to fix syntax error
    private static readonly double[] AvailableDiameters = { 3, 3.5, 4, 4.5, 5, 5.56, 6, 6.625, 8, 8.625, 10.75, 12.75 };
    private static readonly string[] DiameterLabels = { "3 (tube)", "3.5 (tube)", "4 (tube)", "4.5 (tube)", "5 (tube)", "5.56 (tube)", "6 (tube)", "6.625 (6in. pipe)", "8 (tube)", "8.625 (8in. pipe)", "10.75 (10in. pipe)", "12.75 (12in. pipe)" };

    // Add missing methods
    public static int CalculateNumberOfTreads(double overallHeight)
    {
        const double maxRiserHeight = 7.75;  // Per IRC R311.7.10.1
        int numTreads = (int)Math.Ceiling(overallHeight / maxRiserHeight);
        return Math.Max(2, numTreads);  // Minimum 2 treads for stability
    }

    public class StaircaseParameters
    {
        public double WalklineRadius { get; set; }
        // Add other properties if needed based on flow chart
    }

    public StaircaseParameters CalculateParameters()
    {
        return new StaircaseParameters
        {
            WalklineRadius = (_outsideDia / 2) - (_centerPoleDiameter / 2) + 12
        };
    }

    public static bool IsHeightRequiringMidLanding(double overallHeight)
    {
        return overallHeight > 151;  // Per IRC requirements
    }

    public static double CalculateTreadHeight(double overallHeight, int numTreads)
    {
        return overallHeight / numTreads;  // Simple division, ensure it doesn't exceed maxRiserHeight
    }

    public static double CalculateRotationPerTread(double rotationDeg, int numTreads)
    {
        return rotationDeg / numTreads;
    }

    public static double CalculateTreadWidth(double outsideDia, double centerPoleDia)
    {
        return (outsideDia / 2) - (centerPoleDia / 2) - 1.5;  // Based on clear width logic in code
    }

    [CommandMethod("CreateSpiralStaircase")]
        public static void CreateSpiralStaircase()
        {
            Document acadDoc = acadApp.DocumentManager.MdiActiveDocument;
            Database db = acadDoc.Database;
            Editor ed = acadDoc.Editor;

            try
            {
                // Set drawing units to decimal inches
                acadApp.SetSystemVariable("LUNITS", 2);
                acadApp.SetSystemVariable("INSUNITS", 1);
                ed.WriteMessage("\nDrawing units set to decimal inches for this script.");

                // Show user form and get inputs
                using (var form = new UserForm())
                {
                    if (form.ShowDialog() != DialogResult.OK || !form.FormSubmitted)
                    {
                        ed.WriteMessage("\nScript aborted by user.");
                        return;
                    }

                    double centerPoleDia = form.CenterPoleDia;
                    double overallHeight = form.OverallHeight;
                    double outsideDia = form.OutsideDia;
                    double rotationDeg = form.RotationDeg;
                    bool isClockwise = form.IsClockwise;

                    // Validate and adjust inputs
                    centerPoleDia = ValidateCenterPoleDiameter(centerPoleDia, ed);
                    if (!ValidateOverallHeight(overallHeight, ed) ||
                        !ValidateOutsideDiameter(outsideDia, centerPoleDia, ed) ||
                        !ValidateRotationDegrees(rotationDeg, ed))
                    {
                        return;
                    }

                    // Use new methods for calculations
                    int numTreads = CalculateNumberOfTreads(overallHeight);  // Use local variable from form
                    var parameters = new StaircaseParameters { WalklineRadius = (outsideDia / 2) - (centerPoleDia / 2) + 12 };  // Inline calculation as static alternative
                    // Duplicate line removed to resolve naming conflict
                    double walklineRadius = parameters.WalklineRadius;


                    // Check walkline radius
                    if (walklineRadius > 24.5)
                    {
                        double suggestedDia = (24.5 - 12) * 2;
                        PromptResult pr = ed.GetKeywords("\nWalkline Radius Violation: Current walkline radius is " + walklineRadius.ToString("F2") + " inches, exceeding the maximum of 24.5 inches. Suggested center pole diameter: " + suggestedDia.ToString("F2") + " inches.", new string[] { "OK", "Cancel" });
                        if (pr.StringResult == "Cancel")
                        {
                            ed.WriteMessage("\nScript aborted by user.");
                            return;
                        }
                    }

                    // Check clear width
                    double clearWidth = (outsideDia / 2) - (centerPoleDia / 2) - 1.5;
                    if (clearWidth < 26)
                    {
                        double suggestedOutsideDia = (26 + centerPoleDia / 2 + 1.5) * 2;
                        PromptResult pr = ed.GetKeywords("\nClear Width Violation: Current clear width is " + clearWidth.ToString("F2") + " inches, less than the minimum of 26 inches. Suggested outside diameter: " + suggestedOutsideDia.ToString("F2") + " inches.", new string[] { "OK", "Cancel" });
                        if (pr.StringResult == "Cancel")
                        {
                            ed.WriteMessage("\nScript aborted by user.");
                            return;
                        }
                    }

                    double treadWidth = CalculateTreadWidth(outsideDia, centerPoleDia);  // Use local variables
                    double riserHeight = CalculateTreadHeight(overallHeight, numTreads);  // Use local variable
                    double treadAngle = CalculateRotationPerTread(rotationDeg, numTreads);  // Use local variable


                    // Check walkline width
                    double walklineWidth = walklineRadius * (Math.Abs(treadAngle) * Math.PI / 180);
                    if (walklineWidth < 6.75)
                    {
                        double minRotationDeg = 90 + (6.75 / walklineRadius) * (180 / Math.PI) * (numTreads - 1);
                        double minCenterPoleDia = (6.75 * 180 / Math.PI / Math.Abs(treadAngle) - 12) * 2;
                        double suggestedDia = AvailableDiameters.OrderBy(d => Math.Abs(d - minCenterPoleDia)).First();

                        PromptResult pr = ed.GetKeywords("\nWalkline Width Violation: Current walkline width is " + walklineWidth.ToString("F2") + " inches, less than the minimum of 6.75 inches. Options: Retry (auto-adjust), Ignore, Abort.", new string[] { "Retry", "Ignore", "Abort" });
                        if (pr.StringResult == "Retry")
                        {
                            PromptResult pr2 = ed.GetKeywords("\nChoose option: 1 - Increase center pole diameter to " + suggestedDia.ToString("F2") + ", 2 - Increase rotation degrees to " + minRotationDeg.ToString("F2") + ".", new string[] { "1", "2" });
                            if (pr2.StringResult == "1")
                            {
                                centerPoleDia = suggestedDia;
                                walklineRadius = centerPoleDia / 2 + 12;
                                walklineWidth = walklineRadius * (Math.Abs(treadAngle) * Math.PI / 180);
                            }
                            else if (pr2.StringResult == "2")
                            {
                                rotationDeg = minRotationDeg;
                                treadAngle = rotationDeg / numTreads;
                                walklineWidth = walklineRadius * (Math.Abs(treadAngle) * Math.PI / 180);
                            }
                            else
                            {
                                ed.WriteMessage("\nScript aborted by user.");
                                return;
                            }
                        }
                        else if (pr.StringResult == "Abort")
                        {
                            ed.WriteMessage("\nScript aborted by user.");
                            return;
                        }
                    }

                    int midlandingIndex = -1;
                    if (IsHeightRequiringMidLanding(overallHeight))  // Use local variable
                    {
                        // Prompt logic remains, but set midlandingIndex based on user input
                        // (Existing code handles this, so no change here beyond the check)
                        PromptResult pr = ed.GetKeywords("\nOverall height exceeds 151 inches. A midlanding is required. Options: Retry (specify tread number), Ignore, Abort.", new string[] { "Retry", "Ignore", "Abort" });
                        if (pr.StringResult == "Retry")
                        {
                            PromptIntegerOptions pio = new PromptIntegerOptions("\nEnter tread number for midlanding (1 to " + numTreads + "):")
                            {
                                LowerLimit = 1,
                                UpperLimit = numTreads
                            };
                            PromptIntegerResult pir = ed.GetInteger(pio);
                            if (pir.Status == PromptStatus.OK)
                            {
                                midlandingIndex = pir.Value - 1;
                            }
                            else
                            {
                                ed.WriteMessage("\nScript aborted by user.");
                                return;
                            }
                        }
                        else if (pr.StringResult == "Abort")
                        {
                            ed.WriteMessage("\nScript aborted by user.");
                            return;
                        }
                    }

                    // Create staircase
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                        double direction = isClockwise ? 1 : -1;
                        CreateCenterPole(btr, tr, centerPoleDia, overallHeight);

                        int numRegularTreads = numTreads - 1;
                        if (midlandingIndex >= 0)
                        {
                            treadAngle = direction * Math.Abs((rotationDeg - 90) / (numRegularTreads - 1));
                        }
                        else
                        {
                            treadAngle = direction * Math.Abs(rotationDeg / numRegularTreads);
                        }

                        CreateTreads(btr, tr, numTreads, riserHeight, overallHeight, outsideDia, centerPoleDia, treadAngle, midlandingIndex, direction);

                        tr.Commit();
                    }

                    // Regenerate and zoom
                    acadDoc.SendStringToExecute("REGEN\n", true, false, false);
                    acadDoc.SendStringToExecute("ZOOM E\n", true, false, false);

                    // Display success message
                    string midlandingStatus = midlandingIndex >= 0 ? "Yes at tread " + (midlandingIndex + 1) : "No";
                    ed.WriteMessage($"\nSpiral Staircase Created Successfully:\nCenter Pole Diameter: {centerPoleDia:F2} inches\nOverall Height: {overallHeight:F2} inches\nOutside Diameter: {outsideDia:F2} inches\nTotal Rotation: {rotationDeg:F2} degrees\nNumber of Treads: {numTreads}\nRiser Height: {riserHeight:F2} inches\nTread Angle: {treadAngle:F2} degrees\nWalkline Width: {walklineWidth:F2} inches\nMidlanding: {midlandingStatus}");

                    // Create text in drawing
                    using (Transaction tr = db.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;
                        BlockTableRecord btr = tr.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;
                        CreateTextInDrawing(btr, tr, centerPoleDia, overallHeight, outsideDia, rotationDeg, numTreads, riserHeight, treadAngle, walklineWidth, midlandingStatus);
                        tr.Commit();
                    }
                }
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nError occurred: {ex.Message} (Error Number: {ex.HResult})");
            }
        }

        private static double ValidateCenterPoleDiameter(double centerPoleDia, Editor ed)
        {
            if (!AvailableDiameters.Any(d => Math.Abs(d - centerPoleDia) < 0.001))
            {
                double closestDia = AvailableDiameters.OrderBy(d => Math.Abs(d - centerPoleDia)).First();
                ed.WriteMessage($"\nCenter pole diameter adjusted to closest available size: {closestDia:F2} inches. Available options: {string.Join(", ", DiameterLabels)}");
                return closestDia;
            }
            return centerPoleDia;
        }

        private static bool ValidateOverallHeight(double overallHeight, Editor ed)
        {
            if (overallHeight < 20 || overallHeight > 300)
            {
                ed.WriteMessage("\nOverall height must be between 20 and 300 inches.");
                return false;
            }
            return true;
        }

        private static bool ValidateOutsideDiameter(double outsideDia, double centerPoleDia, Editor ed)
        {
            double minOutsideDia = centerPoleDia + 10;
            if (outsideDia < minOutsideDia || outsideDia > 120)
            {
                ed.WriteMessage($"\nOutside diameter must be between {minOutsideDia:F2} and 120 inches.");
                return false;
            }
            return true;
        }

        private static bool ValidateRotationDegrees(double rotationDeg, Editor ed)
        {
            if (rotationDeg < 90 || rotationDeg > 1080)
            {
                ed.WriteMessage("\nTotal rotation must be between 90 and 1080 degrees.");
                return false;
            }
            return true;
        }

        private static void CreateCenterPole(BlockTableRecord btr, Transaction tr, double centerPoleDia, double overallHeight)
        {
            using (Solid3d centerPole = new Solid3d())
            {
                centerPole.CreateFrustum(overallHeight, centerPoleDia / 2, centerPoleDia / 2, 0);
                centerPole.ColorIndex = 251;
                centerPole.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, overallHeight / 2)));
                btr.AppendEntity(centerPole);
                tr.AddNewlyCreatedDBObject(centerPole, true);
            }
        }

        private static void CreateTreads(BlockTableRecord btr, Transaction tr, int numTreads, double riserHeight, double overallHeight, double outsideDia, double centerPoleDia, double treadAngle, int midlandingIndex, double direction)
        {
            double currentAngle = 0;

            for (int i = 0; i < numTreads; i++)
            {
                double treadHeight = riserHeight * (i + 1) - 0.25;
                if (treadHeight > overallHeight - 0.25)
                {
                    treadHeight = overallHeight - 0.25;
                }

                if (i == numTreads - 1)
                {
                    // Create top landing
                    double width = 50;
                    double length = outsideDia / 2;
                    Point3d[] points = new Point3d[4];
                    points[0] = new Point3d(0, 0, treadHeight);
                    points[1] = new Point3d(width, 0, treadHeight);
                    points[2] = new Point3d(width, length, treadHeight);
                    points[3] = new Point3d(0, length, treadHeight);

                    using (Polyline pline = new Polyline())
                    {
                        for (int j = 0; j < 4; j++)
                        {
                            pline.AddVertexAt(j, new Point2d(points[j].X, points[j].Y), 0, 0, 0);
                        }
                        pline.Closed = true;
                        btr.AppendEntity(pline);
                        tr.AddNewlyCreatedDBObject(pline, true);

                        using (Solid3d solid = new Solid3d())
                        {
                            using (DBObjectCollection dbObjects = Region.CreateFromCurves(new DBObjectCollection { pline }))
                            {
                                if (dbObjects.Count > 0)
                                {
                                    using (Region region = (Region)dbObjects[0])
                                    {
                                        solid.Extrude(region, 0.25, 0);
                                    }
                                }
                            }
                            solid.ColorIndex = 3;
                            btr.AppendEntity(solid);
                            tr.AddNewlyCreatedDBObject(solid, true);
                        }
                    }
                }
                else if (i == midlandingIndex)
                {
                    // Create midlanding
                    double endAngle = currentAngle + 90 * direction;
                    CreateTreadRegion(btr, tr, centerPoleDia, outsideDia, treadHeight, currentAngle, endAngle, 1);
                    currentAngle = endAngle;
                }
                else
                {
                    // Create regular tread
                    double endAngle = currentAngle + treadAngle * Math.PI / 180;
                    CreateTreadRegion(btr, tr, centerPoleDia, outsideDia, treadHeight, currentAngle, endAngle, 251);
                    currentAngle = endAngle;
                }
            }
        }

        private static void CreateTreadRegion(BlockTableRecord btr, Transaction tr, double centerPoleDia, double outsideDia, double treadHeight, double startAngle, double endAngle, int colorIndex)
        {
            double innerRadius = centerPoleDia / 2;
            double outerRadius = outsideDia / 2;

            Point3d innerStart = new Point3d(innerRadius * Math.Cos(startAngle), innerRadius * Math.Sin(startAngle), treadHeight);
            Point3d innerEnd = new Point3d(innerRadius * Math.Cos(endAngle), innerRadius * Math.Sin(endAngle), treadHeight);
            Point3d outerStart = new Point3d(outerRadius * Math.Cos(startAngle), outerRadius * Math.Sin(startAngle), treadHeight);
            Point3d outerEnd = new Point3d(outerRadius * Math.Cos(endAngle), outerRadius * Math.Sin(endAngle), treadHeight);

            using (Polyline pline = new Polyline())
            {
                pline.AddVertexAt(0, new Point2d(innerStart.X, innerStart.Y), 0, 0, 0);
                pline.AddVertexAt(1, new Point2d(innerEnd.X, innerEnd.Y), 0, 0, 0);
                pline.AddVertexAt(2, new Point2d(outerEnd.X, outerEnd.Y), 0, 0, 0);
                pline.AddVertexAt(3, new Point2d(outerStart.X, outerStart.Y), 0, 0, 0);
                pline.Closed = true;
                btr.AppendEntity(pline);
                tr.AddNewlyCreatedDBObject(pline, true);

                using (Solid3d solid = new Solid3d())
                {
                    using (DBObjectCollection dbObjects = Region.CreateFromCurves(new DBObjectCollection { pline }))
                    {
                        if (dbObjects.Count > 0)
                        {
                            using (Region region = (Region)dbObjects[0])
                            {
                                solid.Extrude(region, 0.25, 0);
                            }
                        }
                    }
                    solid.ColorIndex = colorIndex;
                    btr.AppendEntity(solid);
                    tr.AddNewlyCreatedDBObject(solid, true);
                }
            }
        }

        private static void CreateTextInDrawing(BlockTableRecord btr, Transaction tr, double centerPoleDia, double overallHeight, double outsideDia, double rotationDeg, int numTreads, double riserHeight, double treadAngle, double walklineWidth, string midlandingStatus)
        {
            double yPos = 10;
            double xPos = outsideDia / 2 + 10;
            double textHeight = 2.5;

            string[] texts = new string[]
            {
                $"Center Pole Diameter: {centerPoleDia:F2} inches",
                $"Overall Height: {overallHeight:F2} inches",
                $"Outside Diameter: {outsideDia:F2} inches",
                $"Total Rotation: {rotationDeg:F2} degrees",
                $"Number of Treads: {numTreads}",
                $"Riser Height: {riserHeight:F2} inches",
                $"Tread Angle: {treadAngle:F2} degrees",
                $"Walkline Width: {walklineWidth:F2} inches",
                $"Midlanding: {midlandingStatus}"
            };

            foreach (string text in texts)
            {
                using (DBText dbText = new DBText())
                {
                    dbText.Position = new Point3d(xPos, yPos, 0);
                    dbText.Height = textHeight;
                    dbText.TextString = text;
                    btr.AppendEntity(dbText);
                    tr.AddNewlyCreatedDBObject(dbText, true);
                }
                yPos -= textHeight * 1.2;
            }
        }
    }
}