using System;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;

namespace SpiralStaircasePlugin
{
    public class StaircaseObjectCreator
    {
        public StaircaseObjectCreator()
        {
        }

        public void CreateStaircase(double centerPoleDiameter, double overallHeight, double outsideDiameter, double totalRotation)
        {
            // Note: This code assumes correct references to AutoCAD .NET API for the target version (2025).
            // If compilation errors occur related to types like MarshalByRefObject or ICloneable,
            // ensure that the project references the correct AutoCAD assemblies (accoremgd.dll, acdbmgd.dll, acmgd.dll)
            // for .NET Framework 4.8.

            // Add null checks and diagnostic logging to handle potential issues with document access
            if (Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager == null)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Error: Application context not initialized.");
                return;
            }

            var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("Error: No active document found.");
                return;
            }

            using (var db = doc.Database)
            using (var tr = db.TransactionManager.StartTransaction())
            {
                try
                {
                    BlockTableRecord btr = (BlockTableRecord)tr.GetObject(
                        SymbolUtilityServices.GetBlockModelSpaceId(db),
                        OpenMode.ForWrite);

                    CreateCenterPole(btr, tr, centerPoleDiameter, overallHeight);
                    CreateTreads(btr, tr, centerPoleDiameter, overallHeight, outsideDiameter, totalRotation);
                    if (StaircaseCalculator.IsHeightRequiringMidLanding(overallHeight))
                    {
                        CreateMidLanding(btr, tr, centerPoleDiameter, overallHeight, outsideDiameter);
                    }
                    CreateTopLanding(btr, tr, centerPoleDiameter, overallHeight, outsideDiameter);

                    tr.Commit();
                }
                catch (System.Exception ex)
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog($"Error during object creation: {ex.Message}");
                    tr.Abort();
                }
            }
        }

        private void CreateCenterPole(BlockTableRecord btr, Transaction tr, double diameter, double height)
        {
            using (var pole = new Solid3d())
            {
                // Use CreateFrustum with equal radii to ensure a uniform cylinder, not tapered.
                pole.CreateFrustum(height, diameter / 2.0, diameter / 2.0, diameter / 2.0);
                // Position the pole with base at z=0. Assuming CreateFrustum places base at origin.
                pole.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, 0)));
                btr.AppendEntity(pole);
                tr.AddNewlyCreatedDBObject(pole, true);
            }
        }

        private void CreateTreads(BlockTableRecord btr, Transaction tr, double centerPoleDiameter, double overallHeight, double outsideDiameter, double totalRotation)
        {
            int numberOfTreads = StaircaseCalculator.CalculateNumberOfTreads(overallHeight);
            double treadHeight = StaircaseCalculator.CalculateTreadHeight(overallHeight, numberOfTreads);
            double rotationPerTread = StaircaseCalculator.CalculateRotationPerTread(totalRotation, numberOfTreads);
            double treadWidth = StaircaseCalculator.CalculateTreadWidth(outsideDiameter, centerPoleDiameter);

            for (int i = 0; i < numberOfTreads; i++)
            {
                double zPosition = i * treadHeight;
                double rotation = i * rotationPerTread;

                // Create tread as a sector shape (pie slice) to match spiral staircase design
                Polyline treadOutline = new Polyline();
                double innerRadius = centerPoleDiameter / 2.0;
                double outerRadius = outsideDiameter / 2.0;
                int arcSegments = 5; // Number of segments to approximate the arc
                double startAngle = rotation;
                double endAngle = rotation + rotationPerTread;
                double angleStep = (endAngle - startAngle) / arcSegments;

                // Add inner point at start angle
                treadOutline.AddVertexAt(0, new Point2d(
                    innerRadius * Math.Cos(startAngle),
                    innerRadius * Math.Sin(startAngle)
                ), 0, 0, 0);

                // Add points along the outer arc from start to end angle
                for (int j = 0; j <= arcSegments; j++)
                {
                    double angle = startAngle + j * angleStep;
                    treadOutline.AddVertexAt(j + 1, new Point2d(
                        outerRadius * Math.Cos(angle),
                        outerRadius * Math.Sin(angle)
                    ), 0, 0, 0);
                }

                // Add inner point at end angle to close the sector
                treadOutline.AddVertexAt(arcSegments + 2, new Point2d(
                    innerRadius * Math.Cos(endAngle),
                    innerRadius * Math.Sin(endAngle)
                ), 0, 0, 0);
                treadOutline.Closed = true;

                // Position the tread at the correct height
                treadOutline.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, zPosition)));

                btr.AppendEntity(treadOutline);
                tr.AddNewlyCreatedDBObject(treadOutline, true);
            }
        }

        private void CreateMidLanding(BlockTableRecord btr, Transaction tr, double centerPoleDiameter, double overallHeight, double outsideDiameter)
        {
            double midHeight = overallHeight / 2.0;
            double landingRadius = outsideDiameter / 2.0;

            // Create a simple circular landing at mid-height
            using (var landingCircle = new Solid3d())
            {
                landingCircle.CreateFrustum(0.25, landingRadius, landingRadius, 0.0);
                landingCircle.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, midHeight)));
                btr.AppendEntity(landingCircle);
                tr.AddNewlyCreatedDBObject(landingCircle, true);
            }
        }

        private void CreateTopLanding(BlockTableRecord btr, Transaction tr, double centerPoleDiameter, double overallHeight, double outsideDiameter)
        {
            double landingRadius = outsideDiameter / 2.0;

            // Create a simple circular landing at the top
            using (var landingCircle = new Solid3d())
            {
                landingCircle.CreateFrustum(0.25, landingRadius, landingRadius, 0.0);
                landingCircle.TransformBy(Matrix3d.Displacement(new Vector3d(0, 0, overallHeight)));
                btr.AppendEntity(landingCircle);
                tr.AddNewlyCreatedDBObject(landingCircle, true);
            }
        }
    }
}