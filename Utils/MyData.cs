﻿namespace AutoFish.Utils;

public class MyData
{
    //玩家数据表
    public List<ItemData> Items { get; set; } = new List<ItemData>();

    #region 数据结构
    public class ItemData
    {
        //玩家名字
        public string Name { get; set; }

        //玩家开关
        public bool Enabled { get; set; } = false;

        //消费模式开关
        public bool Mod { get; set; } = false;

        //记录时间
        public DateTime LogTime { get; set; }

        public ItemData(string name = "", bool enabled = true, bool mod = true)
        {
            Name = name ?? "";
            Enabled = enabled;
            Mod = mod;
        }
    }
    #endregion
}