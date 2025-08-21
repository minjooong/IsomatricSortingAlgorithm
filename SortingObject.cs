using System;
using System.Collections.Generic;
using UnityEngine;

public class SortingObject : MonoBehaviour
{
    [SerializeField] public bool isMovable;
    [SerializeField] public SortType sortType = SortType.Point;
    [SerializeField] public Transform pivot1;
    [SerializeField] public Transform pivot2;
    [SerializeField] public Renderer rendererToSort;
    [SerializeField] public Renderer childRenderer;

    [NonSerialized] public Bounds2D bounds2D = new Bounds2D();
    [NonSerialized] public bool isRegistered = false;

    [NonSerialized]
    public readonly List<SortingObject> staticDependencies = new List<SortingObject>(4);
    [NonSerialized]
    public readonly List<SortingObject> inverseStaticDependencies = new List<SortingObject>(4);
    [NonSerialized]
    public readonly List<SortingObject> movingDependencies = new List<SortingObject>(8);

    public Vector2 SortingPoint1 => pivot1.position;
    public Vector2 SortingPoint2 => pivot2.position;
    public Vector2 AsPoint => (SortingPoint1 + SortingPoint2) / 2;

    private bool applicationIsQuitting = false;

    public void OnEnable()
    {
        if (pivot2 == null) pivot2 = pivot1;
        UpdatePosition();
        SortingManager.Instance.RegisterSprite(this);
        isRegistered = true;
    }

    public void OnDisable()
    {
        if (applicationIsQuitting) return;

        SortingManager.Instance.UnregisterSprite(this);
        isRegistered = false;
    }

    public void RefreshSortingOrder()
    {
        if (applicationIsQuitting) return;
        if (!UpdatePosition()) return;

        SortingManager.Instance.RegisterSprite(this);
        isRegistered = true;
    }

    public bool UpdatePosition()
    {
        if (!transform.hasChanged) return false;

        if (rendererToSort) bounds2D.SetData(rendererToSort.bounds);
        transform.hasChanged = false;

        return true;
    }

    public void SetMovable(bool movable)
    {
        SortingManager.Instance.UnregisterSprite(this);
        isRegistered = false;
        isMovable = movable;
        SortingManager.Instance.RegisterSprite(this);
        isRegistered = true;
    }

    public void SetSortingOrder(int order)
    {
        rendererToSort.sortingOrder = order;
        if (childRenderer) childRenderer.sortingOrder = order + 1;
    }

    void OnApplicationQuit()
    {
        applicationIsQuitting = true;
    }

}

public enum SortType
{
    Point,
    Line
}