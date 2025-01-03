using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ScenePerfomanceTool
{
    public class HotmapData : MonoBehaviour
    {
        [HideInInspector]
        public string sceneName;
        public int interval = 30;
        protected bool isRunning = false;
        protected string local_Game_quality = "";
        protected string version = "";
        protected int currentRecord = 0;
        protected PerformanceData perfData = new PerformanceData();
        // Start is called before the first frame update
        void Start()
        {
            perfData.OnInit();
            perfData.StartRecord();
        }

        public string GetData()
        {
            return perfData.GetPerformaneceData();
        }
        
        // Update is called once per frame
        void Update()
        {
            perfData.OnUpdate();
            //todo:采样点遍历
            if (currentRecord == 0)
            {
                //todo: 记录性能数据
                currentRecord = interval;
                return;
            }

            if (currentRecord!=0)
            {
                currentRecord--;
            }
        }
    }
}
