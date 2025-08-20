using System.Collections.Generic;
using System.Diagnostics;
using GameLogic.Core.Optimization;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SortingManager : SingletonBehaviour<SortingManager>
{
    private static readonly List<SortingObject> staticSpriteList = new List<SortingObject>(64);
    private static readonly List<SortingObject> moveableSpriteList = new List<SortingObject>(16);

    #region [REGISTRATION]
    public void RegisterSprite(SortingObject newSprite)
    {
        if (newSprite.isRegistered)
        {
            UnregisterSprite(newSprite);
        }
        
        if (newSprite.isMovable)
        {
            moveableSpriteList.Add(newSprite);
        }
        else
        {
            SetupStaticDependencies(newSprite);
            staticSpriteList.Add(newSprite);
        }
    }
    
    private void SetupStaticDependencies(SortingObject newSprite)
    {
        int theCount = staticSpriteList.Count;
        for (int i = 0; i < theCount; i++)
        {
            SortingObject otherSprite = staticSpriteList[i];
            
            Bounds2D b1 = newSprite.bounds2D;
            Bounds2D b2 = otherSprite.bounds2D;
            
            if (b1.Intersects(b2))
            {
                int compareResult = CompareIsoSorters(newSprite, otherSprite);
                // Debug.Log("Compared: " + newSprite.gameObject.name + " other: " + otherSprite.gameObject.name + " result: " + compareResult);

                if (compareResult == -1)
                {
                    otherSprite.staticDependencies.Add(newSprite);
                    newSprite.inverseStaticDependencies.Add(otherSprite);
                }
                else if (compareResult == 1)
                {
                    newSprite.staticDependencies.Add(otherSprite);
                    otherSprite.inverseStaticDependencies.Add(newSprite);
                }
            }
        }
    }
    
    public void UnregisterSprite(SortingObject spriteToRemove) {
        if (spriteToRemove.isMovable)
        {
            moveableSpriteList.Remove(spriteToRemove);
        }
        else
        {
            staticSpriteList.Remove(spriteToRemove);
            RemoveStaticDependencies(spriteToRemove);
        }
    }
    
    private void RemoveStaticDependencies(SortingObject spriteToRemove)
    {
        for (int i = 0; i < spriteToRemove.inverseStaticDependencies.Count; i++)
        {
            SortingObject otherSprite = spriteToRemove.inverseStaticDependencies[i];
            otherSprite.staticDependencies.Remove(spriteToRemove);
        }
        spriteToRemove.inverseStaticDependencies.Clear();
        spriteToRemove.staticDependencies.Clear();
    }
    
    #endregion
    
    #region [UPDATE]
    
    void Update()
    {
        UpdateSorting();
    }
    
    private readonly List<SortingObject> sortedSprites = new List<SortingObject>(64);
    public void UpdateSorting()
    {
        // 움직이는 오브젝트 위치 갱신
        for (int i = 0; i < moveableSpriteList.Count; i++)
        {
            moveableSpriteList[i].UpdatePosition();
        }
        
        // 움직이는 오브젝트 Order 초기화
        for (int i = 0; i < staticSpriteList.Count; i++)
        {
            staticSpriteList[i].movingDependencies.Clear();
        }
        for (int i = 0; i < moveableSpriteList.Count; i++)
        {
            moveableSpriteList[i].movingDependencies.Clear();
        }
        
        // Order 다시 계산 후 등록
        AddMovingDependencies(moveableSpriteList, staticSpriteList);

        sortedSprites.Clear();
        TopologicalSort.Sort(staticSpriteList, moveableSpriteList, sortedSprites);
        SetSortOrderBasedOnListOrder(sortedSprites);
    }
    
    private void AddMovingDependencies(List<SortingObject> moveableList, List<SortingObject> staticList)
    {
        int moveableCount = moveableList.Count;
        int staticCount = staticList.Count;
        for (int i = 0; i < moveableCount; i++)
        {
            SortingObject moveSprite1 = moveableList[i];
            
            //Add Moving Dependencies to static sprites
            for (int j = 0; j < staticCount; j++)
            {
                SortingObject staticSprite = staticList[j];
                if (moveSprite1.bounds2D.Intersects(staticSprite.bounds2D))
                {
                    int compareResult = CompareIsoSorters(moveSprite1, staticSprite);
                    if (compareResult == -1)
                    {
                        staticSprite.movingDependencies.Add(moveSprite1);
                    }
                    else if (compareResult == 1)
                    {
                        moveSprite1.movingDependencies.Add(staticSprite);
                    }
                }
            }
            //Add Moving Dependencies to Moving Sprites
            for (int j = 0; j < moveableCount; j++)
            {
                SortingObject moveSprite2 = moveableList[j];
                if (moveSprite1.bounds2D.Intersects(moveSprite2.bounds2D))
                {
                    int compareResult = CompareIsoSorters(moveSprite1, moveSprite2);
                    if (compareResult == -1)
                    {
                        moveSprite2.movingDependencies.Add(moveSprite1);
                    }
                }
            }
        }
    }

    private void SetSortOrderBasedOnListOrder(List<SortingObject> spriteList)
    {
        int orderCurrent = 0;
        int count = spriteList.Count;
        for (int i = 0; i < count; ++i)
        {
            spriteList[i].SetSortingOrder(orderCurrent);
            // Debug.Log(spriteList[i].name + " : " + orderCurrent);
            orderCurrent += 2;
        }
    }
    #endregion
    
    #region [SORTING ALGORITHM]
    
    /// <summary>
    /// 두 개의 SortingObject 순서를 비교
    /// </summary>
    /// <param name="object1"></param>
    /// <param name="object2"></param>
    /// <returns>
    /// -1 : object1이 object2보다 뒤에 있다
    ///  0 : 같다
    ///  1 : object1이 object2보다 앞에 있다
    /// </returns>
    private int CompareIsoSorters(SortingObject object1, SortingObject object2)
    {
        if (object1.sortType == SortType.Point && object2.sortType == SortType.Point)
        {
            int result = object2.SortingPoint1.y.CompareTo(object1.SortingPoint1.y);
            return result != 0 ? result : object2.SortingPoint1.x.CompareTo(object1.SortingPoint1.x);
        }        
        if (object1.sortType == SortType.Line && object2.sortType == SortType.Line)
            return CompareLineAndLine(object1, object2);
        
        if (object1.sortType == SortType.Point && object2.sortType == SortType.Line)
            return ComparePointAndLine(object1.SortingPoint1, object2);
        
        if (object1.sortType == SortType.Line && object2.sortType == SortType.Point)
            return -ComparePointAndLine(object2.SortingPoint1, object1);
        
        return 0;
    }
    
    private int CompareLineAndLine(SortingObject line1, SortingObject line2)
    {
        Vector2 line1Point1 = line1.SortingPoint1;
        Vector2 line1Point2 = line1.SortingPoint2;
        Vector2 line2Point1 = line2.SortingPoint1;
        Vector2 line2Point2 = line2.SortingPoint2;

        int comp1 = ComparePointAndLine(line1Point1, line2);
        int comp2 = ComparePointAndLine(line1Point2, line2);
        int oneVStwo = int.MinValue;
        if (comp1 == comp2) oneVStwo = comp1;

        int comp3 = ComparePointAndLine(line2Point1, line1);
        int comp4 = ComparePointAndLine(line2Point2, line1);
        int twoVSone = int.MinValue;
        if (comp3 == comp4) twoVSone = -comp3;
        
        if (oneVStwo != int.MinValue && twoVSone != int.MinValue)
        {
            if (oneVStwo == twoVSone) return oneVStwo;
            return CompareLineCenters(line1, line2);
        }
        
        if (oneVStwo != int.MinValue) return oneVStwo;
        if (twoVSone != int.MinValue) return twoVSone;
        
        return CompareLineCenters(line1, line2);
    }

    private int CompareLineCenters(SortingObject line1, SortingObject line2)
    {
        return -line1.AsPoint.y.CompareTo(line2.AsPoint.y);
    }

    private int ComparePointAndLine(Vector3 point, SortingObject line) {
        float pointY = point.y;
        if (pointY > line.SortingPoint1.y && pointY > line.SortingPoint2.y) return -1;
        if (pointY < line.SortingPoint1.y && pointY < line.SortingPoint2.y) return 1;
  
        float slope = (line.SortingPoint2.y - line.SortingPoint1.y) /
                      (line.SortingPoint2.x - line.SortingPoint1.x);
        float intercept = line.SortingPoint1.y - (slope * line.SortingPoint1.x); // line 그래프의 y절편
        float yOnLineForPoint = (slope * point.x) + intercept; // x = point.x 그래프와 line 그래프 교점의 y값
        return yOnLineForPoint > point.y ? 1 : -1;
    }
    
    #endregion
    
}

public static class TopologicalSort
{
    private static readonly Dictionary<int, bool> circularDepData = new Dictionary<int, bool>();
    private static readonly List<SortingObject> circularDepStack = new List<SortingObject>(64);

    private static readonly HashSet<int> visited = new HashSet<int>();
    private static readonly List<SortingObject> allSprites = new List<SortingObject>(64);
    public static List<SortingObject> Sort(List<SortingObject> staticSprites, List<SortingObject> movableSprites, List<SortingObject> sorted)
    {
        allSprites.Clear();
        allSprites.AddRange(movableSprites);
        allSprites.AddRange(staticSprites);

        int allSpriteCount = allSprites.Count;

        for (int i = 0; i < 5; i++)
        {
            circularDepStack.Clear();
            circularDepData.Clear();
            bool removedDependency = false;
            for (int j = 0; j < allSpriteCount; j++)
            {
                if (RemoveCircularDependencies(allSprites[j], circularDepStack, circularDepData))
                {
                    removedDependency = true;
                }
            }
            if (!removedDependency)
            {
                break;
            }
        }

        visited.Clear();
        for (int i = 0; i < allSpriteCount; i++)
        {
            Visit(allSprites[i], sorted, visited);
        }

        return sorted;
    }

    private static void Visit(SortingObject item, List<SortingObject> sorted, HashSet<int> visited)
    {
        int id = item.GetInstanceID();
        if (!visited.Contains(id))
        {
            visited.Add(id);

            List<SortingObject> dependencies = item.movingDependencies;
            int dcount = dependencies.Count;
            for (int i = 0; i < dcount; i++)
            {
                Visit(dependencies[i], sorted, visited);
            }
            dependencies = item.staticDependencies;
            dcount = dependencies.Count;
            for (int i = 0; i < dcount; i++)
            {
                Visit(dependencies[i], sorted, visited);
            }

            sorted.Add(item);
        }
    }

    private static bool RemoveCircularDependencies(SortingObject item, List<SortingObject> _circularDepStack, Dictionary<int, bool> _circularDepData)
    {
        _circularDepStack.Add(item);
        bool removedDependency = false;

        int id = item.GetInstanceID();
        bool alreadyVisited = _circularDepData.TryGetValue(id, out bool inProcess);
        if (alreadyVisited)
        {
            if (inProcess)
            {
                RemoveCircularDependencyFromStack(_circularDepStack);
                removedDependency = true;
            }
        }
        else
        {
            _circularDepData[id] = true;

            List<SortingObject> dependencies = item.movingDependencies;
            for (int i = 0; i < dependencies.Count; i++)
            {
                if (RemoveCircularDependencies(dependencies[i], _circularDepStack, _circularDepData))
                {
                    removedDependency = true;
                }
            }
            dependencies = item.staticDependencies;
            for (int i = 0; i < dependencies.Count; i++)
            {
                if (RemoveCircularDependencies(dependencies[i], _circularDepStack, _circularDepData))
                {
                    removedDependency = true;
                }
            }

            _circularDepData[id] = false;
        }

        _circularDepStack.RemoveAt(_circularDepStack.Count - 1);
        return removedDependency;
    }

    private static void RemoveCircularDependencyFromStack(List<SortingObject> _circularReferenceStack)
    {
        if (_circularReferenceStack.Count > 1)
        {
            SortingObject startingSorter = _circularReferenceStack[_circularReferenceStack.Count - 1];
            int repeatIndex = 0;
            for (int i = _circularReferenceStack.Count - 2; i >= 0; i--)
            {
                SortingObject sorter = _circularReferenceStack[i];
                if (sorter == startingSorter)
                {
                    repeatIndex = i;
                    break;
                }
            }

            int weakestDepIndex = -1;
            float longestDistance = float.MinValue;
            for (int i = repeatIndex; i < _circularReferenceStack.Count - 1; i++)
            {
                SortingObject sorter1a = _circularReferenceStack[i];
                SortingObject sorter2a = _circularReferenceStack[i + 1];
                if (sorter1a.sortType == SortType.Point && sorter2a.sortType == SortType.Point)
                {
                    float dist = UnityEngine.Mathf.Abs(sorter1a.AsPoint.x - sorter2a.AsPoint.x);
                    if (dist > longestDistance)
                    {
                        weakestDepIndex = i;
                        longestDistance = dist;
                    }
                }
            }
            if (weakestDepIndex == -1)
            {
                for (int i = repeatIndex; i < _circularReferenceStack.Count - 1; i++)
                {
                    SortingObject sorter1a = _circularReferenceStack[i];
                    SortingObject sorter2a = _circularReferenceStack[i + 1];
                    float dist = UnityEngine.Mathf.Abs(sorter1a.AsPoint.x - sorter2a.AsPoint.x);
                    if (dist > longestDistance)
                    {
                        weakestDepIndex = i;
                        longestDistance = dist;
                    }
                }
            }
            SortingObject sorter1 = _circularReferenceStack[weakestDepIndex];
            SortingObject sorter2 = _circularReferenceStack[weakestDepIndex + 1];
            sorter1.movingDependencies.Remove(sorter2);
            sorter1.movingDependencies.Remove(sorter2);
        }
    }
}
