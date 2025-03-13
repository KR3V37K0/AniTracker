using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class CustomGridLayout : LayoutGroup
{
    public Vector2 spacing = Vector2.zero;

    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        CalculateLayout();
        CalculatePreferredSize();
    }

    public override void CalculateLayoutInputVertical()
    {
        CalculateLayout();
        CalculatePreferredSize();
    }

    public override void SetLayoutHorizontal()
    {
        CalculateLayout();
    }

    public override void SetLayoutVertical()
    {
        CalculateLayout();
    }

    private void CalculateLayout()
    {
        int childCount = rectChildren.Count;
        if (childCount == 0)
            return;

        // ��������� padding
        float width = rectTransform.rect.width - padding.left - padding.right;
        float height = rectTransform.rect.height - padding.top - padding.bottom;

        // ������������ ��������� ������� � ������ padding
        Vector2 startPosition = new Vector2(padding.left, -padding.top);

        float currentX = startPosition.x;
        float currentY = startPosition.y;
        float maxRowHeight = 0;

        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = rectChildren[i];

            // �������� ����������� ������ � ������ ��������� ��������
            float childWidth = LayoutUtility.GetPreferredWidth(child);
            float childHeight = LayoutUtility.GetPreferredHeight(child);

            // ���� ������� �� ���������� � ������� ������, ��������� �� ����� ������
            if (currentX + childWidth > width)
            {
                currentX = startPosition.x;
                currentY -= maxRowHeight + spacing.y;
                maxRowHeight = 0;
            }

            // ������������� ������� � ������ ��������� ��������
            SetChildAlongAxis(child, 0, currentX, childWidth);
            SetChildAlongAxis(child, 1, -currentY, childHeight);

            // ��������� ������� ������� � ������������ ������ ������
            currentX += childWidth + spacing.x;
            maxRowHeight = Mathf.Max(maxRowHeight, childHeight);
        }
    }

    private void CalculatePreferredSize()
    {
        int childCount = rectChildren.Count;
        if (childCount == 0)
            return;

        float width = rectTransform.rect.width - padding.left - padding.right;
        float height = rectTransform.rect.height - padding.top - padding.bottom;

        Vector2 startPosition = new Vector2(padding.left, -padding.top);

        float currentX = startPosition.x;
        float currentY = startPosition.y;
        float maxRowHeight = 0;
        float totalHeight = 0;

        for (int i = 0; i < childCount; i++)
        {
            RectTransform child = rectChildren[i];

            float childWidth = LayoutUtility.GetPreferredWidth(child);
            float childHeight = LayoutUtility.GetPreferredHeight(child);

            if (currentX + childWidth > width)
            {
                currentX = startPosition.x;
                currentY -= maxRowHeight + spacing.y;
                totalHeight += maxRowHeight + spacing.y;
                maxRowHeight = 0;
            }

            currentX += childWidth + spacing.x;
            maxRowHeight = Mathf.Max(maxRowHeight, childHeight);
        }

        // ��������� ������ ��������� ������
        totalHeight += maxRowHeight;

        // ������������� ���������������� �������
        SetLayoutInputForAxis(totalHeight, totalHeight, -1, 1); // Vertical
        SetLayoutInputForAxis(width, width, -1, 0); // Horizontal
    }
}