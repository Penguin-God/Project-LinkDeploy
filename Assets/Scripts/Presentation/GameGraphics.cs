using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class GameGraphics
{
    public static readonly Color Ink = new Color32(10, 19, 27, 255);
    public static readonly Color Panel = new Color32(17, 30, 39, 248);
    public static readonly Color Border = new Color32(64, 87, 94, 255);
    public static readonly Color Paper = new Color32(230, 238, 228, 255);
    public static readonly Color Muted = new Color32(141, 164, 170, 255);
    public static readonly Color Gold = new Color32(246, 183, 75, 255);
    public static readonly Color Cyan = new Color32(95, 216, 211, 255);
    public static readonly Color Green = new Color32(162, 218, 119, 255);
    public static readonly Color Red = new Color32(235, 103, 87, 255);
    public static TMP_FontAsset Font;

    public static RectTransform Rect(string name, Transform parent, float left, float top, float width, float height)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        var rect = gameObject.GetComponent<RectTransform>(); rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(left, -top); rect.sizeDelta = new Vector2(width, height);
        return rect;
    }
    public static Image Image(string name, Transform parent, float left, float top, float width, float height, Color color, Sprite sprite = null)
    {
        var image = Rect(name, parent, left, top, width, height).gameObject.AddComponent<Image>();
        image.color = color; image.sprite = sprite; image.raycastTarget = false;
        return image;
    }
    public static RectTransform Box(string name, Transform parent, float left, float top, float width, float height, Color? fill = null, Color? border = null)
    {
        var outer = Image(name, parent, left, top, width, height, border ?? Border);
        Image("Surface", outer.transform, 1, 1, width - 2, height - 2, fill ?? Panel);
        return outer.rectTransform;
    }
    public static TextMeshProUGUI Text(string name, Transform parent, float left, float top, float width, float height, string value, float size = 20, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var text = Rect(name, parent, left, top, width, height).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = Font; text.fontSize = size; text.color = color ?? Paper; text.text = value;
        text.alignment = alignment; text.raycastTarget = false; text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis; text.richText = true;
        return text;
    }
    public static RectTransform Frame(string name,Transform parent,float left,float top,float width,float height,Color fill,Color border)
    {
        var rect=Rect(name,parent,left,top,width,height);
        Image("Fill",rect,0,0,width,height,fill);
        Image("Top",rect,0,0,width,1,border);Image("Bottom",rect,0,height-1,width,1,border);
        Image("Left",rect,0,0,1,height,border);Image("Right",rect,width-1,0,1,height,border);
        return rect;
    }
    public static Button Button(string name, Transform parent, float left, float top, float width, float height, string label, UnityEngine.Events.UnityAction action, Color? fill = null, float size = 19)
    {
        var rect = Box(name, parent, left, top, width, height, fill ?? new Color32(30, 49, 60, 255));
        var image = rect.GetComponent<Image>(); image.raycastTarget = true;
        var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
        var colors = button.colors; colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f); colors.pressedColor = Cyan; colors.disabledColor = new Color(.35f,.4f,.42f); button.colors = colors;
        Text("Label", rect, 5, 0, width - 10, height, label, size, null, TextAlignmentOptions.Center);
        button.onClick.AddListener(action); return button;
    }
}

// Vector overlays join independent sprite bodies into a continuous conveyor network.
public class PipelineGraphic : MaskableGraphic
{
    public struct Segment { public Vector2 start; public Vector2 end; public Color color; public float width; public bool arrows; }
    public List<Segment> segments = new List<Segment>();
    public float phase;
    public bool drawTracks = true;
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        foreach (var segment in segments)
        {
            if(drawTracks)
            {
                Line(mesh, segment.start, segment.end, segment.width + 5, new Color32(13, 22, 28, 255));
                Line(mesh, segment.start, segment.end, segment.width, new Color32(80, 94, 94, 255));
                Line(mesh, segment.start, segment.end, segment.width - 5, segment.color * new Color(1,1,1,.7f));
            }
            if (!segment.arrows) continue;
            var direction = (segment.end - segment.start).normalized;
            var normal = new Vector2(-direction.y, direction.x);
            float length = Vector2.Distance(segment.start, segment.end);
            for (float distance = (phase % 1) * 22; distance < length; distance += 22)
            {
                Vector2 center = segment.start + direction * distance;
                Triangle(mesh, center + direction * 6, center - direction * 4 + normal * 4, center - direction * 4 - normal * 4, segment.color);
            }
        }
    }
    static void Line(VertexHelper mesh, Vector2 start, Vector2 end, float width, Color color)
    {
        var normal = new Vector2(-(end - start).y, (end - start).x).normalized * width * .5f;
        int index = mesh.currentVertCount;
        mesh.AddVert(start + normal, color, Vector2.zero); mesh.AddVert(end + normal, color, Vector2.zero);
        mesh.AddVert(end - normal, color, Vector2.zero); mesh.AddVert(start - normal, color, Vector2.zero);
        mesh.AddTriangle(index, index + 1, index + 2); mesh.AddTriangle(index, index + 2, index + 3);
    }
    static void Triangle(VertexHelper mesh, Vector2 tip, Vector2 left, Vector2 right, Color color)
    {
        int index = mesh.currentVertCount;
        mesh.AddVert(tip,color,Vector2.zero); mesh.AddVert(left,color,Vector2.zero); mesh.AddVert(right,color,Vector2.zero);
        mesh.AddTriangle(index,index+1,index+2);
    }
}

public class RangeGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear(); const int segments = 128;
        float radius = rectTransform.rect.width * .5f;
        Vector2 center = rectTransform.rect.center;
        for (int index = 0; index < segments; index++)
        {
            float angle = index * Mathf.PI * 2 / segments, next = (index + 1) * Mathf.PI * 2 / segments;
            Vector2 first = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), second = new Vector2(Mathf.Cos(next), Mathf.Sin(next));
            int start = mesh.currentVertCount;
            mesh.AddVert(center, new Color(color.r,color.g,color.b,.025f), Vector2.zero);
            mesh.AddVert(center + first * radius, new Color(color.r,color.g,color.b,.055f), Vector2.zero);
            mesh.AddVert(center + second * radius, new Color(color.r,color.g,color.b,.055f), Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2);
            if (index % 4 == 3) continue;
            start = mesh.currentVertCount;
            mesh.AddVert(center + first * radius,color,Vector2.zero); mesh.AddVert(center + second * radius,color,Vector2.zero);
            mesh.AddVert(center + second * (radius - 2),color,Vector2.zero); mesh.AddVert(center + first * (radius - 2),color,Vector2.zero);
            mesh.AddTriangle(start,start+1,start+2); mesh.AddTriangle(start,start+2,start+3);
        }
    }
}
