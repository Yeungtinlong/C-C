using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BlockMapDebugUI : MonoBehaviour
{
    [SerializeField] private Text _text;
    [SerializeField] private Image _bg;
    [SerializeField] private Color _freeColor;
    [SerializeField] private Color _restrictColor;

    private bool _isInitialized;
    private Vector2Int _blockMapPos;

    public void SetupDebugUI(Vector2Int blockMapPos, Vector3 transformPos)
    {
        if (!_isInitialized)
        {
            _blockMapPos = blockMapPos;
            _text.text = _blockMapPos.x + ", " + _blockMapPos.y;
            transform.position = transformPos;
            _isInitialized = true;
        }
    }

    public void UpdateColor(bool isFree)
    {
        if (isFree)
        {
            _bg.color = _freeColor;
        }
        else
        {
            _bg.color = _restrictColor;
        }
    }
}
