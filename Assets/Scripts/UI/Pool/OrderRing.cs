using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(MeshRenderer))]
public class OrderRing : MonoBehaviour
{
    [SerializeField] private Shader _shader;
    [SerializeField] private Color _color;

    private Material _ringMat;
    
    private MeshRenderer _renderer;

    public event UnityAction<OrderRing> OnRelease;

    private void Awake()
    {
        _ringMat = new Material(_shader);
        _renderer = GetComponent<MeshRenderer>();
        _renderer.material = _ringMat;
    }

    private void Update()
    {
        _ringMat.SetColor("_Color", _color);
    }

    public void ReleaseObject()
    {
        if (OnRelease != null)
        {
            OnRelease.Invoke(this);
        }
    }
}