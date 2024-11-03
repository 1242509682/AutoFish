using Newtonsoft.Json;
using TShockAPI;

namespace AutoFish
{
    internal class Configuration
    {
        #region 实例变量
        [JsonProperty("插件开关", Order = -12)]
        public bool Enabled { get; set; } = true;

        [JsonProperty("多钩钓鱼", Order = -11)]
        public bool MoreHook { get; set; } = true;

        [JsonProperty("多钩上限", Order = -10)]
        public int HookMax { get; set; } = 5;

        [JsonProperty("鱼饵数量", Order = -9)]
        public int BaitStack { get; set; } = 10;

        [JsonProperty("自动时长", Order = -8)]
        public int timer { get; set; } = 24;

        [JsonProperty("广告开关", Order = -7)]
        public bool AdvertisementEnabled { get; set; } = true;

        [JsonProperty("广告内容", Order = -6)]
        public string Advertisement { get; set; } = $"\n[i:3456][C/F2F2C7:插件开发] [C/BFDFEA:by] [c/00FFFF:羽学] | [c/7CAEDD:少司命][i:3459]";

        [JsonProperty("指定鱼饵", Order = -5)]
        public int[] BaitType { get; set; } = { 2002, 2675, 2676, 3191, 3194 };

        [JsonProperty("指定渔获", Order = -4)]
        public List<int> DoorItems = new();
        #endregion



        #region 读取与创建配置文件方法
        public static readonly string FilePath = Path.Combine(TShock.SavePath, "自动钓鱼.json");

        public void Write()
        {
            var json = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(FilePath, json);
        }

        public static Configuration Read()
        {
            if (!File.Exists(FilePath))
            {
                var NewConfig = new Configuration();
                new Configuration().Write();
                return NewConfig;
            }
            else
            {
                var jsonContent = File.ReadAllText(FilePath);
                return JsonConvert.DeserializeObject<Configuration>(jsonContent)!;
            }
        }
        #endregion

    }
}