using UnityEngine;

public class ScrollableComboBox
{
    private static bool forceToUnShow = false;
    private static int useControlID = -1;
    public bool isClickedComboButton = false;
    public int selectedItemIndex = 0;
    public Vector2 scrollPos = new Vector2(0.0f, 0.0f);
    private Vector2 scrollPosOld = new Vector2(0.0f, 0.0f);
    public float height;
    public bool wasChanged = false;

    public int List(UnityEngine.Rect rect, string buttonText, GUIContent[] listContent, GUIStyle listStyle)
    {
        return this.List(rect, new GUIContent(buttonText), listContent, (GUIStyle)"button", (GUIStyle)"box", listStyle);
    }

    public int List(UnityEngine.Rect rect, GUIContent buttonContent, GUIContent[] listContent, GUIStyle listStyle)
    {
        return this.List(rect, buttonContent, listContent, (GUIStyle)"button", (GUIStyle)"box", listStyle);
    }

    public int List(UnityEngine.Rect rect, string buttonText, GUIContent[] listContent, GUIStyle buttonStyle, GUIStyle boxStyle, GUIStyle listStyle)
    {
        return this.List(rect, new GUIContent(buttonText), listContent, buttonStyle, boxStyle, listStyle);
    }

    private int GetPix(int i)
    {
        return (int)((1.0 + ((double)Screen.width / 1280.0 - 1.0) * 0.600000023841858) * (double)i);
    }

    public int List(UnityEngine.Rect rect, GUIContent buttonContent, GUIContent[] listContent, GUIStyle buttonStyle, GUIStyle boxStyle, GUIStyle listStyle)
    {
        this.wasChanged = false;
        if (ScrollableComboBox.forceToUnShow)
        {
            ScrollableComboBox.forceToUnShow = false;
            this.isClickedComboButton = false;
        }
        bool flag = false;
        int controlId = GUIUtility.GetControlID(FocusType.Passive);
        if (UnityEngine.Event.current.GetTypeForControl(controlId) == UnityEngine.EventType.MouseUp && this.isClickedComboButton && this.scrollPosOld == this.scrollPos)
            flag = true;
        if (GUI.Button(rect, buttonContent, buttonStyle))
        {
            if (ScrollableComboBox.useControlID == -1)
            {
                ScrollableComboBox.useControlID = controlId;
                this.isClickedComboButton = false;
            }
            if (ScrollableComboBox.useControlID != controlId)
            {
                ScrollableComboBox.forceToUnShow = true;
                ScrollableComboBox.useControlID = controlId;
            }
            this.isClickedComboButton = true;
        }
        if (this.isClickedComboButton)
        {
            UnityEngine.Rect position = new UnityEngine.Rect(rect.x, rect.y + (float)this.GetPix(23), rect.width, listStyle.CalcHeight(listContent[0], 1f) * (float)listContent.Length);
            if ((double)position.y + (double)position.height > (double)this.height)
            {
                position.height = (float)((double)this.height - (double)position.y - 2.0);
                position.width += 16f;
            }
            GUI.Box(position, "", boxStyle);
            if (Input.GetMouseButtonDown(0))
                this.scrollPosOld = this.scrollPos;
            UnityEngine.Rect rect1 = new UnityEngine.Rect(0, 200 + listStyle.CalcHeight(listContent[0], 1f), rect.width, listStyle.CalcHeight(listContent[0], 1f) * (float)listContent.Length);
            this.scrollPos = GUI.BeginScrollView(position, this.scrollPos, rect1);
            int num = GUI.SelectionGrid(rect1, this.selectedItemIndex, listContent, 1, listStyle);
            if (num != this.selectedItemIndex)
                this.selectedItemIndex = num;
            GUI.EndScrollView();
        }
        if (flag)
        {
            this.isClickedComboButton = false;
            this.wasChanged = true;
        }
        return this.GetSelectedItemIndex();
    }

    public int GetSelectedItemIndex()
    {
        return this.selectedItemIndex;
    }
}