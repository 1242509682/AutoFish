using AutoFish.Utils;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace MHookFish;

[ApiVersion(2, 1)]
public class MHookFish : TerrariaPlugin
{

    #region 插件信息
    public override string Name => "多线钓鱼";
    public override string Author => "羽学";
    public override Version Version => new Version(1, 0, 0);
    public override string Description => "涡轮增压不蒸鸭";
    #endregion

    #region 注册与释放
    public MHookFish(Main game) : base(game) { }
    public override void Initialize()
    {
        LoadConfig();
        GeneralHooks.ReloadEvent += ReloadConfig;
        GetDataHandlers.NewProjectile += ProjectNew!;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            GeneralHooks.ReloadEvent -= ReloadConfig;
            GetDataHandlers.NewProjectile -= ProjectNew!;
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

    #region 鱼线上钩处理
    public void ProjectNew(object sender, GetDataHandlers.NewProjectileEventArgs e)
    {
        var plr = e.Player;

        if (plr == null || !plr.Active || !plr.IsLoggedIn || !Config.Enabled || !plr.HasPermission("mhookfish"))
        {
            return;
        }

        // 检查浮漂是否上钩
        if (BobbersActive(e.Owner))
        {
            // 控制浮漂的上限
            var HookCount = Main.projectile.Count(p => p.active && p.owner == e.Owner && p.bobber);
            if (HookCount > Config.maxHooks - 1)
            {
                return;
            }

            for (var j = 0; j < Config.ProjData.Count; j++)
            {
                var proj = Config.ProjData[j];
                var speed = proj.Speed > 0f ? e.Velocity.ToLenOf(proj.Speed) : e.Velocity;
                var guid = Guid.NewGuid().ToString();

                foreach (var id in proj.ID)
                {
                    var index = SpawnProjectile.NewProjectile(Main.projectile[e.Index].GetProjectileSource_FromThis(), e.Position, speed, id, (int)e.Damage, e.Knockback, e.Owner, proj.AI[0], proj.AI[1], proj.AI[2], -1, guid);

                    plr.SetBuff(80, 10, false);
                    plr.SendData(PacketTypes.ProjectileNew, "", index);

                    // 更新多线计数
                    HookCount++;
                }
            }
        }
    }
    #endregion

    #region 检查浮漂是否跳动
    public static bool BobbersActive(int whoAmI)
    {
        using (IEnumerator<Projectile> enumerator = Main.projectile.Where((Projectile p) => p.active && p.owner == whoAmI && p.bobber).GetEnumerator())
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