using System;
using Autodesk.AutoCAD.Runtime;

namespace SpiralStaircasePlugin
{
    public class ValidationHandler
    {
        // Flow chart ranges:
        private const double MinOverallHeight = 20.0; // inches
        private const double MaxOverallHeight = 300.0; // inches
        private const double MinOutsideDiameterRelativeOffset = 10.0; // inches (outsideDia must be at least centerPoleDia + 10)
        private const double MaxOutsideDiameter = 120.0; // inches (10 feet)
        private const double MinTotalRotation = 90.0; // degrees
        private const double MaxTotalRotation = 1080.0; // degrees (3 full rotations)
        // Note: Flow chart mentioned specific available center pole diameters, not a simple range. Removing strict range check for now.

        public bool ValidateInputs(double centerPoleDiameter, double overallHeight, double outsideDiameter, double totalRotation, out string errorMessage)
        {
            errorMessage = string.Empty;

            // Validate Overall Height (Flow chart: 20 to 300)
            if (overallHeight < MinOverallHeight || overallHeight > MaxOverallHeight)
            {
                errorMessage = $"Overall height must be between {MinOverallHeight} and {MaxOverallHeight} inches.";
                return false;
            }

            // Validate Outside Diameter (Flow chart: centerPoleDia + 10 to 120)
            double minRequiredOutsideDiameter = centerPoleDiameter + MinOutsideDiameterRelativeOffset;
            if (outsideDiameter < minRequiredOutsideDiameter || outsideDiameter > MaxOutsideDiameter)
            {
                errorMessage = $"Outside diameter must be between {minRequiredOutsideDiameter:F2} (Center Pole Dia + {MinOutsideDiameterRelativeOffset}) and {MaxOutsideDiameter} inches.";
                return false;
            }

            // Validate Rotation Degrees (Flow chart: 90 to 1080)
            if (totalRotation < MinTotalRotation || totalRotation > MaxTotalRotation)
            {
                errorMessage = $"Total rotation must be between {MinTotalRotation} and {MaxTotalRotation} degrees.";
                return false;
            }

            // Additional validation for IRC R311.7.10.1 compliance (Clear Walking Path Width)
            double clearWidth = (outsideDiameter / 2.0) - (centerPoleDiameter / 2.0);
            const double MinClearWidth = 26.0; // inches per IRC R311.7.10.1
            if (clearWidth < MinClearWidth)
            {
                errorMessage = $"The clear walking path width ({clearWidth:F2} inches) is less than the minimum required {MinClearWidth} inches (IRC R311.7.10.1). Increase Outside Diameter or decrease Center Pole Diameter.";
                return false;
            }

            return true;
        }

        public bool RequiresMidLanding(double overallHeight)
        {
            return overallHeight > 147.0; // Mid-landing required if height exceeds 147 inches (12.25 feet)
        }
    }
}