using Terraria;
using TShockAPI;

namespace AutoFish.Utils
{
    internal class Tools
    {
        #region 辅助方法：查找鱼饵在背包中的位置
        public static int GetBait(TSPlayer ply, int itemType)
        {
            for (int i = 0; i < ply.TPlayer.inventory.Length; i++)
            {
                if (ply.TPlayer.inventory[i].type == itemType)
                {
                    return i;
                }
            }
            return -1;
        }
        #endregion

        #region 更新字典:把鱼饵的物品和数量记录下来
        public static void UpDict(Dictionary<int, int> Bait, int type, int stack)
        {
            //如果ID已经在字典里
            if (Bait.ContainsKey(type))
            {
                // 给这个ID加数量
                Bait[type] += stack;
            }

            // ID不在字典里
            else
            {
                // 直接添加新ID和它的数量
                Bait.Add(type, stack);
            }
        }
        #endregion

        #region 判断浮漂跳动
        public static bool BobbersActive(int whoAmI)
        {
            using (IEnumerator<Projectile> enumerator = Main.projectile.Where((p) => p.active && p.owner == whoAmI && p.bobber).GetEnumerator())
            {
                if (enumerator.MoveNext())
                {
                    _ = enumerator.Current;
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
