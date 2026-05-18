using System;
using UnityEditor;

namespace Fix.Editor
{
    public abstract class FixEditorBase
    {
        protected const string FixRoot = FixEditorConst.FixRoot;

        protected static void AskForAction(Action action, string title = null, string message = null)
        {
            if (title == null) title = nameof(AskForAction);
            if (message == null) message = "确认执行?";
            if (EditorUtility.DisplayDialog(title, message, "确认", "取消"))
            {
                action.Invoke();
            }
        }
    }
}