using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Runtime;

namespace SpiralStaircasePlugin
{
    public class InputHandler
    {
        public double CenterPoleDia { get; private set; }
        public double OverallHeight { get; private set; }
        public double OutsideDia { get; private set; }
        public double RotationDeg { get; private set; }
        public bool IsClockwise { get; private set; }  // Based on PRD direction input

        public bool GetUserInputs()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;

            // Prompt for Center Pole Diameter
            var optCenterPoleDia = new PromptDoubleOptions("\nEnter center pole diameter: ");
            optCenterPoleDia.AllowNone = false;
            var resCenterPoleDia = ed.GetDouble(optCenterPoleDia);
            if (resCenterPoleDia.Status != PromptStatus.OK) return false;
            CenterPoleDia = resCenterPoleDia.Value;

            // Prompt for Overall Height
            var optOverallHeight = new PromptDoubleOptions("\nEnter overall height (inches): ");
            optOverallHeight.AllowNone = false;
            var resOverallHeight = ed.GetDouble(optOverallHeight);
            if (resOverallHeight.Status != PromptStatus.OK) return false;
            OverallHeight = resOverallHeight.Value;

            // Prompt for Outside Diameter
            var optOutsideDia = new PromptDoubleOptions("\nEnter outside diameter (inches): ");
            optOutsideDia.AllowNone = false;
            var resOutsideDia = ed.GetDouble(optOutsideDia);
            if (resOutsideDia.Status != PromptStatus.OK) return false;
            OutsideDia = resOutsideDia.Value;

            // Prompt for Total Rotation
            var optRotationDeg = new PromptDoubleOptions("\nEnter total rotation (degrees): ");
            optRotationDeg.AllowNone = false;
            var resRotationDeg = ed.GetDouble(optRotationDeg);
            if (resRotationDeg.Status != PromptStatus.OK) return false;
            RotationDeg = resRotationDeg.Value;

            // Prompt for Rotation Direction
            var optDirection = new PromptKeywordOptions("\nEnter rotation direction [Clockwise/CounterClockwise]: ", "Clockwise CounterClockwise");
            optDirection.AllowNone = false;
            var resDirection = ed.GetKeywords(optDirection);
            if (resDirection.Status != PromptStatus.OK) return false;
            IsClockwise = resDirection.StringResult == "Clockwise";

            return true;  // All inputs successfully obtained
        }
    }
}