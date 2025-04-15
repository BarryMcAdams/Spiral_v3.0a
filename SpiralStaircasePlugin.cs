using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput; // Added for Editor
using Autodesk.AutoCAD.Geometry;   // Added for Point2d, Extents3d
using Autodesk.AutoCAD.Runtime;
using System;

namespace SpiralStaircasePlugin
{
    public class SpiralStaircasePlugin
    {
        // Main entry point for the plugin
        [CommandMethod("CreateSpiralStaircase")]
        public void CreateSpiralStaircase()
        {
            try
            {
                if (Application.DocumentManager == null)
                {
                    Application.ShowAlertDialog("Error: Application context not initialized.");
                    return;
                }
                var doc = Application.DocumentManager.MdiActiveDocument;
                if (doc == null)
                {
                    Application.ShowAlertDialog("Error: No active document found.");
                    return;
                }
                var ed = doc.Editor;  // Declare here for broader scope
                var db = doc.Database;  // Declare here for broader scope

                // Set units using direct API calls within a transaction
                if (db != null)
                {
                    using (var tr = db.TransactionManager.StartTransaction())
                    {
                        db.Lunits = 2; // Decimal
                        db.Insunits = UnitsValue.Inches; // Inches
                        tr.Commit();
                    }
                    ed.WriteMessage("\nDrawing units set to decimal inches.");
                }

                // --- Hardcoded Inputs for Testing ---
                double centerPoleDia = 5.62;
                double overallHeight = 144.0;
                double outsideDia = 72.0;
                double totalRotation = 450.0;
                bool isClockwise = true; // Assuming Clockwise for now
                doc.Editor.WriteMessage($"\nUsing hardcoded values: PoleDia={centerPoleDia}, Height={overallHeight}, OutsideDia={outsideDia}, Rotation={totalRotation}, Clockwise={isClockwise}");
                // --- End Hardcoded Inputs ---

                // Validate inputs
                var validationHandler = new ValidationHandler();
                string errorMessage;
                if (!validationHandler.ValidateInputs(centerPoleDia, overallHeight, outsideDia, totalRotation, out errorMessage))
                {
                    if (ed != null)
                    {
                        ed.WriteMessage($"\nValidation Error: {errorMessage}");
                    }
                    return;
                }

                // Perform calculations
                // Note: StaircaseCalculator might need adjustment if it relies on InputHandler properties directly
                // Assuming StaircaseCalculator takes values in constructor or methods
                var calculator = new StaircaseCalculator(centerPoleDia, overallHeight, outsideDia, totalRotation);
                calculator.CalculateParameters(); // Ensure this method exists and works with constructor values
                var staircaseObjectCreator = new StaircaseObjectCreator();
                staircaseObjectCreator.CreateStaircase(centerPoleDia,
                                              overallHeight,
                                              outsideDia,
                                              totalRotation); // Removed isClockwise as it's not used in the method

                // Create objects step completed above, no need to repeat

                // Output and finalize
                // Zoom Extents using direct API call
                if (db != null && ed != null)
                {
                    ZoomExtents(db, ed);
                    ed.WriteMessage("\nSpiral staircase created successfully.");
                }
            }
            catch (System.Exception ex)
            {
                Application.ShowAlertDialog($"Error: {ex.Message}");
            }
        }

        // Helper function for Zoom Extents using Database Extents
        private void ZoomExtents(Database db, Editor ed)
        {
            // Ensure we are in Model Space for Zoom Extents
            if (db.TileMode)
            {
                try
                {
                    // Use database extents
                    Point3d minPoint = db.Extmin;
                    Point3d maxPoint = db.Extmax;

                    // Check if extents are valid (database extents are initialized even if drawing is empty)
                    // A simple check if they are different might suffice, or if they are not the default large values
                    if (minPoint.DistanceTo(maxPoint) > 1e-6) // Check if extents represent more than just a point
                    {
                        // Set the view
                        using (ViewTableRecord view = ed.GetCurrentView())
                        {
                            // Calculate view parameters from database extents
                            double viewWidth = maxPoint.X - minPoint.X;
                            double viewHeight = maxPoint.Y - minPoint.Y;
                            Point2d viewCenter = new Point2d(
                                (minPoint.X + maxPoint.X) / 2.0,
                                (minPoint.Y + maxPoint.Y) / 2.0);

                            // Add a small buffer/margin
                            viewWidth *= 1.05;
                            viewHeight *= 1.05;

                            // Maintain aspect ratio
                            if (view.Width / view.Height > viewWidth / viewHeight)
                            {
                                viewHeight = viewWidth * view.Height / view.Width;
                            }
                            else
                            {
                                viewWidth = viewHeight * view.Width / view.Height;
                            }

                            view.Width = viewWidth;
                            view.Height = viewHeight;
                            view.CenterPoint = viewCenter;
                            ed.SetCurrentView(view);
                        }
                    }
                    else
                    {
                        // Handle case where extents might be invalid (e.g., empty drawing)
                        ed.WriteMessage("\nWarning: Could not calculate valid extents for Zoom Extents.");
                    }
                }
                catch (System.Exception ex)
                {
                    ed.WriteMessage($"\nError during Zoom Extents: {ex.Message}");
                }
            }
            else
            {
                ed.WriteMessage("\nZoom Extents works best in Model Space (TileMode = On).");
                // Optionally, add logic for Paper Space zoom if needed
            }
        }
    }
}