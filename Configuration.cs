using Newtonsoft.Json;
using TShockAPI;

namespace MHookFish
{
    internal class Configuration
    {
        #region 实例变量
        [JsonProperty("插件开关", Order = -10)]
        public bool Enabled { get; set; } = true;

        [JsonProperty("鱼钩上限", Order = -9)]
        public int maxHooks { get; set; } = 5;
        #endregion

        #region 预设参数方法
        private void Ints()
        {
            this.ProjData = new List<ItemData>
            {
                new ItemData(new int[] {360,361,362,363,364,365,366,381,382,760,775 },
                10f,new float[] { 0.0f, 0.0f, 0.0f }),
            };
        }
        #endregion

        #region 数据结构
        [JsonProperty("鱼钩数据")]
        public List<ItemData> ProjData { get; set; } = new();

        public class ItemData
        {
            [JsonProperty("射速", Order = -7)]
            public float Speed = 10f;

            [JsonProperty("弹幕AI", Order = -6)]
            public float[] AI { get; set; } = new float[3] { 0.0f, 1.0f, 0.0f };

            [JsonProperty("弹幕ID", Order = -5)]
            public int[] ID = new int[] { };

            public ItemData(int[] id, float speed, float[] ai)
            {
                this.ID = id;
                this.Speed = speed;
                this.AI = ai;
            }
        }
        #endregion

        #region 读取与创建配置文件方法
        public static readonly string FilePath = Path.Combine(TShock.SavePath, "多线钓鱼.json");

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
                NewConfig.Ints();
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