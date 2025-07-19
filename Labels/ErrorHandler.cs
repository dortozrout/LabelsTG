using System.Diagnostics;
using Terminal.Gui;

namespace LabelsTG.Labels
{
    static class ErrorHandler
    {
        public static void HandleError(Exception exception)
        {
            var stackTrace = new StackTrace();
            var stackFrame = stackTrace.GetFrame(1); // Get the caller frame
            var methodBase = stackFrame.GetMethod();
            var className = methodBase.DeclaringType.Name;
            var methodName = methodBase.Name;
            View.ShowError($"{className}.{methodName}\n{exception.Message}\n{exception.StackTrace}");
            Environment.Exit(1);
        }
    }
}

