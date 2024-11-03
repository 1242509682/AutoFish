using AutoFish.Utils;
using Terraria;
using Terraria.ID;
using TShockAPI;
using TShockAPI.Hooks;
using TerrariaApi.Server;
using System.Text;

namespace AutoFish;

[ApiVersion(2, 1)]
public class AutoFish : TerrariaPlugin
{

    #region 插件信息
    public override string Name => "自动钓鱼";
    public override string Author => "羽学 少司命";
    public override Version Version => new Version(1, 1, 0);
    public override string Description => "涡轮增压不蒸鸭";
    #endregion

    #region 注册与释放
    public AutoFish(Main game) : base(game) { }
    public override void Initialize()
    {
        LoadConfig();
        GeneralHooks.ReloadEvent += ReloadConfig;
        GetDataHandlers.NewProjectile += ProjectNew!;
        ServerApi.Hooks.ServerJoin.Register(this, this.OnJoin);
        GetDataHandlers.PlayerUpdate.Register(this.OnPlayerUpdate);
        ServerApi.Hooks.ProjectileAIUpdate.Register(this, ProjectAiUpdate);
        TShockAPI.Commands.ChatCommands.Add(new Command("autofish", Commands.Afs, "af"));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            GetDataHandlers.NewProjectile -= ProjectNew!;
            ServerApi.Hooks.ServerJoin.Deregister(this, this.OnJoin);
            GetDataHandlers.PlayerUpdate.UnRegister(this.OnPlayerUpdate);
            ServerApi.Hooks.ProjectileAIUpdate.Deregister(this, ProjectAiUpdate);
            TShockAPI.Commands.ChatCommands.RemoveAll(x => x.CommandDelegate == Commands.Afs);
        }
        base.Dispose(disposing);
    }
    #endregion

    #region 配置重载读取与写入方法
    internal static Configuration Config = new();
    private static void ReloadConfig(ReloadEventArgs args)
    {
        LoadConfig();
        args.Player.SendInfoMessage("[多线钓鱼]重新加载配置完毕。");
    }
    private static void LoadConfig()
    {
        Config = Configuration.Read();
        Config.Write();
    }
    #endregion

    #region 玩家更新配置方法（创建配置结构）
    internal static MyData Data = new();
    private void OnJoin(JoinEventArgs args)
    {
        if (args == null || !Config.Enabled)
        {
            return;
        }

        var plr = TShock.Players[args.Who];

        if (plr == null)
        {
            return;
        }

        // 如果玩家不在数据表中，则创建新的数据条目
        if (!Data.Items.Any(item => item.Name == plr.Name))
        {
            Data.Items.Add(new MyData.ItemData()
            {
                Name = plr.Name,
                Enabled = false,
                AutoFish = false,
                Bait = new Dictionary<int, int> { { 0, 0 }, }
            });
        }
    }
    #endregion

    #region 鱼饵消耗+规避松露虫方法
    private static int ClearCount = 0; //需要关闭钓鱼权限的玩家计数
    private void OnPlayerUpdate(object? sender, GetDataHandlers.PlayerUpdateEventArgs e)
    {
        var plr = e.Player;
        if (!Config.Enabled || e == null || plr == null || !plr.IsLoggedIn || !plr.Active || !plr.HasPermission("autofish"))
        {
            return;
        }

        var list = Data.Items.FirstOrDefault(x => x.Name == plr.Name);

        if (list == null || !list.Enabled )
        {
            return;
        }

        // 玩家自己插件开关
        if (list != null)
        {
            bool flag = false;
            int TotalCount = 0; // 用于记录背包中所有鱼饵的总数量

            // 遍历背包58格
            for (var i = 0; i < plr.TPlayer.inventory.Length; i++)
            {
                var inv = plr.TPlayer.inventory[i];

                if(inv.type == 2673)
                {
                    list.Enabled = false;
                    return;
                }

                // 是指定鱼饵 不是松露虫
                if (Config.BaitType.Contains(inv.type))
                {
                    // 添加到玩家自己的字典
                    Tools.UpDict(list.Bait, inv.type, inv.stack);

                    // 累加背包中的鱼饵数量
                    TotalCount += inv.stack;

                    // 如果总数量超过设定值
                    if (TotalCount >= Config.BaitStack)
                    {
                        // 开启标识
                        flag = true;
                        break;
                    }
                }
            }

            if (flag && !list.AutoFish)
            {
                int Remain = Config.BaitStack;
                var Message = new StringBuilder();

                // 使用 LINQ 遍历并更新库存
                var consumedItems = list.Bait.ToDictionary(
                    pair => pair.Key,
                    pair =>
                    {
                        var index = Tools.GetBait(plr, pair.Key);
                        if (index != -1)
                        {
                            int BaitStack = Math.Min(Remain, plr.TPlayer.inventory[index].stack);

                            plr.TPlayer.inventory[index].stack -= BaitStack;
                            plr.SendData(PacketTypes.PlayerSlot, "", plr.Index, PlayerItemSlotID.Inventory0 + index);

                            // 更新字典中的数量
                            list.Bait[pair.Key] -= BaitStack;
                            if (list.Bait[pair.Key] < 1)
                            {
                                list.Bait.Remove(pair.Key); // 如果数量为0，移除该项
                            }

                            // 记录消耗的鱼饵
                            Message.AppendFormat("{0}({1}),", TShock.Utils.GetItemById(pair.Key).Name, BaitStack);

                            // 减少剩余需要消耗的鱼饵数量
                            Remain -= BaitStack;

                            return BaitStack;
                        }

                        return 0;
                    });

                if (Remain <= 0)
                {
                    // 开启自动钓鱼开关
                    list.AutoFish = true;

                    // 记录当前时间
                    list.LogTime = DateTime.Now;

                    plr.SendMessage($"[c/46C2D4:{plr.Name}] 已开启[c/F5F251:自动钓鱼]功能,消耗鱼饵:{Message}", 247, 244, 150);
                }
            }

        }

        // 检查是否有玩家的自动钓鱼权限需要关闭
        var Remove = Data.Items.Where(list => list != null && list.LogTime != default &&
            (DateTime.Now - list.LogTime).TotalMinutes >= Config.timer).ToList();

        if (Remove != null)
        {
            var mess = new StringBuilder();
            mess.AppendLine($"[i:3455][c/AD89D5:自][c/D68ACA:动][c/DF909A:钓][c/E5A894:鱼][i:3454]");
            mess.AppendLine($"以下玩家超过 [c/E17D8C:{Config.timer}] 分钟 已关闭[c/76D5B4:自动钓鱼]权限：");

            foreach (var plr2 in Remove)
            {
                // 只显示分钟
                var Minutes = (DateTime.Now - plr2.LogTime).TotalMinutes;
                FormattableString minutes = $"{Minutes:F0}";

                // 更新时间超过自动时长 关闭自动钓鱼权限
                if (Minutes >= Config.timer)
                {
                    ClearCount++;
                    plr2.AutoFish = false;
                    plr2.LogTime = default; // 清空记录时间
                    mess.AppendFormat("[c/A7DDF0:{0}]:[c/74F3C9:{1}分钟], ", plr2.Name, minutes);
                }
            }

            // 确保有一个玩家计数，只播报一次
            if (ClearCount > 0 && mess.Length > 0)
            {
                //广告开关
                if (Config.AdvertisementEnabled)
                {
                    //自定义广告内容
                    mess.AppendLine(Config.Advertisement);
                }
                plr.SendMessage(mess.ToString(), 247, 244, 150);
                ClearCount = 0;
            }
        }
    }
    #endregion

    #region 自动钓鱼方法
    private void ProjectAiUpdate(ProjectileAiUpdateEventArgs args)
    {
        if (args.Projectile.owner is < 0 or > Main.maxPlayers ||
            !args.Projectile.active ||
            !args.Projectile.bobber ||
            !Config.Enabled)
            return;

        var ply = TShock.Players[args.Projectile.owner];
        if (ply == null || !ply.Active || !ply.HasPermission("autofish"))
            return;

        var list = Data.Items.FirstOrDefault(x => x.Name == ply.Name);
        if (list == null || !list.Enabled || !list.AutoFish)
        {
            return;
        }

        //玩家的自动钓鱼开关
        if (list.AutoFish && list.Enabled)
        {
            //上钩物品
            if (args.Projectile.ai[1] < 0)
            {
                args.Projectile.ai[0] = 1.0f;
                if (Config.DoorItems.Any())
                {
                    args.Projectile.ai[1] = Convert.ToSingle(Config.DoorItems.OrderByDescending(x => Guid.NewGuid()).First());
                }
                else
                {
                    do
                    {
                        args.Projectile.FishingCheck();
                        args.Projectile.ai[1] = args.Projectile.localAI[1];
                    }
                    while (args.Projectile.ai[1] <= 0);

                }
                ply.SendData(PacketTypes.ProjectileNew, "", args.Projectile.whoAmI);

                var index = SpawnProjectile.NewProjectile(Main.projectile[args.Projectile.whoAmI].GetProjectileSource_FromThis(),
                    args.Projectile.position, args.Projectile.velocity, args.Projectile.type, 0, 0, args.Projectile.owner, 0, 0, 0);

                ply.SendData(PacketTypes.ProjectileNew, "", index);
            }
        }
    }
    #endregion

    #region 多线钓鱼
    public void ProjectNew(object sender, GetDataHandlers.NewProjectileEventArgs e)
    {
        var ply = e.Player;
        var guid = Guid.NewGuid().ToString();
        var HookCount = Main.projectile.Count(p => p.active && p.owner == e.Owner && p.bobber); // 浮漂计数

        if (ply == null ||
            !ply.Active ||
            !ply.IsLoggedIn ||
            !Config.Enabled ||
            !Config.MoreHook ||
            !ply.HasPermission("autofish") ||
            HookCount > Config.HookMax - 1)
            return;

        var list = Data.Items.FirstOrDefault(x => x.Name == ply.Name);
        if (list == null || !list.Enabled || !list.AutoFish)
        {
            return;
        }

        //玩家的自动钓鱼开关
        if (list.AutoFish && list.Enabled)
        {
            // 检查浮漂是否上钩
            if (Tools.BobbersActive(e.Owner))
            {
                var index = SpawnProjectile.NewProjectile(Main.projectile[e.Index].GetProjectileSource_FromThis(),
                    e.Position, e.Velocity, e.Type, (int)e.Damage, e.Knockback, e.Owner, 0, 0, 0, -1, guid);

                ply.SendData(PacketTypes.ProjectileNew, "", index);

                // 更新多线计数
                HookCount++;
            }
        }
    }
    #endregion

}