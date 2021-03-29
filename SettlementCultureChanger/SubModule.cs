using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace SettlementCultureChanger
{
    public class SubModule : MBSubModuleBase
    {
        private Dictionary<string, string> settings = new Dictionary<string, string>();
        public static bool isGradual;
        public static int weeksToPassForChange;
        protected override void OnSubModuleLoad()
        {
            string[] strArray = File.ReadAllText(System.IO.Path.GetFullPath(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "..\\..\\")) + "/Settings.cfg").Split('\n');
            for (int index = 0; index < strArray.Length; ++index)
            {
                if (!strArray[index].StartsWith("//"))
                    this.settings.Add(strArray[index].Split('=')[0], strArray[index].Split('=')[1]);
            }
            isGradual = bool.Parse(this.settings["Gradual"]);
            weeksToPassForChange = int.Parse(this.settings["Weeks"]);
            if (weeksToPassForChange < 0) weeksToPassForChange = 4;
        }
        protected override void OnGameStart(Game game,IGameStarter gameStarter)
        {
            base.OnGameStart(game, gameStarter);
            try
            {
                CampaignGameStarter campaignGameStarter = (CampaignGameStarter)gameStarter;
                campaignGameStarter.AddBehavior(new ChangeSettlementCulture());
            }
            catch (System.InvalidCastException e)
            {
                
            }
            
        } 
    }
}
