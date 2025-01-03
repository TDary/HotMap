using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScenePerfomanceTool
{
    [Serializable]
    public struct SceneSamplePoint
    {
        public string sceneName;
        public List<VectorPoint> allPoints;
    }

    public struct VectorPoint
    {
        public VectorPosition Position;
        public VectorRotation Rotation;
    }
    
    [Serializable]
    public struct VectorPosition
    {
        public float x;
        public float y;
        public float z;
    }
    
    [Serializable]
    public struct VectorRotation
    {
        public float x;
        public float y;
        public float z;
    }
}