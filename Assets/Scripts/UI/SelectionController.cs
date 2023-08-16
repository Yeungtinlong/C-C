using UnityEngine;

public class SelectionController : MonoBehaviour
{
    [Header("Listening to")]
    [SerializeField] private SelectionRectChannelSO _selectionRectChannelSO = default;
    private Texture2D _borderTexture;
    private Rect _rect;
    private bool _isDrawing;

    private void Awake()
    {
        if (_borderTexture == null)
        {
            _borderTexture = new Texture2D(1, 1);
            _borderTexture.SetPixel(0, 0, Color.white);
            _borderTexture.Apply();
        }
    }

    private void OnEnable()
    {
        _selectionRectChannelSO.OnBeginDrawRectRequested += BeginDrawSelectionRect;
        _selectionRectChannelSO.OnStopDrawRectRequested += StopDrawSelectionRect;
    }

    private void OnDisable()
    {
        _selectionRectChannelSO.OnBeginDrawRectRequested -= BeginDrawSelectionRect;
        _selectionRectChannelSO.OnStopDrawRectRequested -= StopDrawSelectionRect;
    }

    private void BeginDrawSelectionRect(Rect rect)
    {
        _isDrawing = true;
        _rect = rect;
    }

    private void StopDrawSelectionRect()
    {
        _isDrawing = false;
    }

    private void DrawSelectionRectBorder(Rect rect, float thickness)
    {
        rect = Rect2GUIRect(rect);
        GUI.color = Color.white;
        // left
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), _borderTexture);
        // bottom
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), _borderTexture);
        // rigth
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), _borderTexture);
        //top
        GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), _borderTexture);
    }

    private void DrawSelectionRect(Rect rect)
    {
        rect = Rect2GUIRect(rect);
        GUI.color = new Color(0f, 1f, 0.1f, 0.15f);
        GUI.DrawTexture(rect, _borderTexture);
    }

    private static Rect Rect2GUIRect(Rect rect)
    {
        float yMin = Screen.height - rect.yMin - rect.height;
        float yMax = yMin + rect.height;
        rect.yMin = yMin;
        rect.yMax = yMax;
        return rect;
    }

    private void OnGUI()
    {
        if (_isDrawing)
        {
            DrawSelectionRect(_rect);
            DrawSelectionRectBorder(_rect, 2f);
        }
    }
}
