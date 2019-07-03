using UnityEngine;
using UnityEditor;

public class PathfindingInfo
{
    public Block Parent;
    public PathfindingState State = PathfindingState.Open;
    public int CurrentDistanceFromStart;
    public int StraightDistanceFromEnd;
    public int StepValue
    {
        get
        {
            return CurrentDistanceFromStart + StraightDistanceFromEnd;
        }
    }
}