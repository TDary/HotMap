using System.Text;
using Unity.Profiling;
using UnityEngine;

namespace ScenePerfomanceTool
{
    public class PerformanceData
    {
        ProfilerRecorder drawCallsRecorder;
        ProfilerRecorder setPassCallsRecorder;
        ProfilerRecorder verticesRecorder;
        ProfilerRecorder trianglesRecorder;
        ProfilerRecorder totalAllocatedMemoryRecorder;
        protected FpsCounter fpsCounter;

        /// <summary>
        /// 初始化函数
        /// </summary>
        public void OnInit()
        {
            fpsCounter = new FpsCounter(1.0f);
        }
    
        /// <summary>
        /// 每帧更新函数
        /// </summary>
        public void OnUpdate()
        {
            fpsCounter.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        public string GetPerformaneceData()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Performance Data:");
            sb.AppendFormat("DrawCalls:{0}", drawCallsRecorder.LastValue);
            sb.AppendFormat("SetPassCalls:{0}", setPassCallsRecorder.LastValue);
            sb.AppendFormat("Vertices:{0}", verticesRecorder.LastValue);
            sb.AppendFormat("Triangles:{0}", trianglesRecorder.LastValue);
            sb.AppendFormat("TotalAllocatedMemory:{0}", totalAllocatedMemoryRecorder.LastValue);
            sb.AppendFormat("Fps:{0}", fpsCounter.CurrentFps);
            return sb.ToString();
        }

        /// <summary>
        /// 初始化赋值并开始采集
        /// </summary>
        public void StartRecord()
        {
            drawCallsRecorder =  ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            setPassCallsRecorder =  ProfilerRecorder.StartNew(ProfilerCategory.Render,"SetPass Calls Count");
            verticesRecorder =  ProfilerRecorder.StartNew(ProfilerCategory.Render,"Vertices Count");
            trianglesRecorder =  ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count");
            totalAllocatedMemoryRecorder =  ProfilerRecorder.StartNew(ProfilerCategory.Memory,"Total Used Memory");
        }

        /// <summary>
        /// 停止采集并释放资源对象
        /// </summary>
        public void StopRecord()
        {
            drawCallsRecorder.Dispose();
            setPassCallsRecorder.Dispose();
            verticesRecorder.Dispose();
            trianglesRecorder.Dispose();
            totalAllocatedMemoryRecorder.Dispose();
        }
    }
}