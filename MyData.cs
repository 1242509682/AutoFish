namespace AutoFish;

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

        //自动钓鱼开关
        public bool AutoFish { get; set; } = false;

        //记录时间
        public DateTime LogTime { get; set; }

        //玩家拥有的鱼饵数量
        public Dictionary<int, int> Bait { get; set; } = new Dictionary<int, int>();

        public ItemData(string name = "", bool enabled = true, bool auto = true, Dictionary<int, int> DelItem = null!)
        {
            this.Name = name ?? "";
            this.Enabled = enabled;
            this.AutoFish = auto;
            this.Bait = DelItem;
        }
    }

    #endregion
}
