using TShockAPI;

namespace AutoFish
{
    public class Commands
    {
        #region 菜单方法
        private static void HelpCmd(TSPlayer player)
        {

            if (player == null)
            {
                return;
            }
            else
            {
                player.SendMessage("【自动钓鱼】[i:3456] 插件开发 [C/F2F2C7:by] [c/00FFFF:羽学]|[c/7CAEDD:少司命][i:3459]\n" +
                 "/af —— 查看自动钓鱼菜单\n" +
                 "/af on —— 开启[c/4686D4:自动钓鱼]功能\n" +
                 "/af off —— 关闭[c/F25055:自动钓鱼]功能", 193, 223, 186);
            }
        }
        #endregion

        public static void Afs(CommandArgs args)
        {
            var name = args.Player.Name;
            var data = AutoFish.Data.Items.FirstOrDefault(item => item.Name == name);

            if (!AutoFish.Config.Enabled)
            {
                return;
            }

            if (data == null)
            {
                args.Player.SendInfoMessage("请用角色[c/D95065:重进服务器]后输入：/af 指令查看菜单\n羽学声明：本插件纯属[c/7E93DE:免费]请勿上当受骗", 217, 217, 217);
                return;
            }

            var Minutes = AutoFish.Config.timer - (DateTime.Now - data.LogTime).TotalMinutes;
            FormattableString minutes = $"{Minutes:F0}";

            if (args.Parameters.Count == 0)
            {
                HelpCmd(args.Player);

                if (!data.Enabled)
                {
                    args.Player.SendSuccessMessage($"请输入该指令开启→: [c/92C5EC:/af on]");
                }
                else
                {
                    args.Player.SendMessage($"自动钓鱼[c/46C4D4:剩余时长]为：[c/F3F292:{minutes}]分钟",243,181,145);
                }
                return;
            }
            if (args.Parameters.Count == 1)
            {
                if (args.Parameters[0].ToLower() == "on")
                {
                    data.Enabled = true;
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:启用]自动钓鱼功能。");
                    return;
                }

                if (args.Parameters[0].ToLower() == "off")
                {
                    data.Enabled = false;
                    args.Player.SendSuccessMessage($"玩家 [{args.Player.Name}] 已[c/92C5EC:禁用]自动钓鱼功能。");
                    return;
                }
            }
        }
    }
}
