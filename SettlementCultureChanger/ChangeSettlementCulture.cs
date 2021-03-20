using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.SandBox.CampaignBehaviors;

namespace SettlementCultureChanger
{
    public class ChangeSettlementCulture : CampaignBehaviorBase
    {
        static Dictionary<Settlement, CultureObject> initialCultureDictionary;
        Dictionary<Settlement, int> settlementTickCounters = new Dictionary<Settlement, int>();
        public override void RegisterEvents()
        {
            if (SubModule.isGradual)
                CampaignEvents.WeeklyTickSettlementEvent.AddNonSerializedListener((object)this, new Action<Settlement>(this.OnSettlementWeeklyTick));
           
            CampaignEvents.OnSiegeAftermathAppliedEvent.AddNonSerializedListener((object)this,new Action<MobileParty, Settlement, SiegeAftermathCampaignBehavior.SiegeAftermath, Clan, Dictionary<MobileParty, float>>(this.OnSiegeAftermathApplied));
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener((object)this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
            CampaignEvents.ClanChangedKingdom.AddNonSerializedListener((object)this, new Action<Clan, Kingdom, Kingdom, bool, bool>(this.OnClanChangedKingdom));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(this.OnGameLoaded));
        }

        private void OnGameLoaded(CampaignGameStarter obj)
        {
            Dictionary<Settlement, CultureObject> initialCultureList = new Dictionary<Settlement, CultureObject>();
            foreach (Settlement settlement in Campaign.Current.Settlements.Where(s => s.IsTown || s.IsCastle || s.IsVillage))
            {
                initialCultureList.Add(settlement, settlement.Culture);
                if (!SubModule.isGradual)
                    ChangeCulture(settlement);
            }
            SetInitialCultureList(initialCultureList);
            if (SubModule.isGradual)
            {
                foreach (Settlement settlement in Campaign.Current.Settlements.Where(s => s.IsTown || s.IsCastle || s.IsVillage))
                {
                    if (settlement.Culture != (settlement.OwnerClan.Kingdom?.Culture ?? settlement.OwnerClan.Culture) && !settlementTickCounters.ContainsKey(settlement))
                    {
                        AddSettlementCounter(settlement);
                    }
                }
            }
        }

        private void OnSiegeAftermathApplied(MobileParty arg1, Settlement settlement, SiegeAftermathCampaignBehavior.SiegeAftermath arg3, Clan arg4, Dictionary<MobileParty, float> arg5)
        {
            if (SubModule.isGradual)
                AddSettlementCounter(settlement);
            else
                ChangeCulture(settlement);
        }

        private void OnSettlementOwnerChanged(Settlement settlement, bool arg2, Hero arg3, Hero arg4, Hero arg5, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (detail != ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
            {
                if (SubModule.isGradual)
                    AddSettlementCounter(settlement);
                else
                    ChangeCulture(settlement);
            }
            else if (detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
            {
                RevertCulture(settlement);
            }
            
        }

        private void OnClanChangedKingdom(Clan clan, Kingdom arg2, Kingdom arg3, bool arg4, bool arg5)
        {
            OnKingdomChange(clan);
        }
        public static void SetInitialCultureList(Dictionary<Settlement, CultureObject> dict) => initialCultureDictionary = dict;
        public static void ChangeCulture(Settlement settlement)
        {
            if (!(settlement.IsVillage || settlement.IsCastle || settlement.IsTown)) { return; }

            if (settlement.Culture != (settlement.OwnerClan.Kingdom?.Culture ?? settlement.OwnerClan.Culture))
            {
                settlement.Culture = settlement.OwnerClan.Kingdom?.Culture ?? settlement.OwnerClan.Culture;
                DeleteNotableTroops(settlement);
                foreach (Village boundVillage in settlement.BoundVillages)
                {
                    ChangeCulture(boundVillage.Settlement);
                }
            }
        }
        public void OnKingdomChange(Clan clan)
        {
            foreach (Settlement settlement in clan.Settlements.Where(s => s.IsTown || s.IsCastle || s.IsVillage))
            {
                if (SubModule.isGradual)
                    AddSettlementCounter(settlement);
                else
                    ChangeCulture(settlement);
            }
        }
        public void RevertCulture(Settlement settlement)
        {
            settlement.Culture = initialCultureDictionary[settlement];
        }
        public static void DeleteNotableTroops(Settlement settlement)
        {
            if (settlement.IsTown || settlement.IsVillage)
            {
                if (settlement.Notables != null)
                {
                    foreach (Hero notable in settlement.Notables)
                    {
                        if (notable.CanHaveRecruits)
                        {
                            for (int index = 0; index < 6; ++index)
                            {
                                notable.VolunteerTypes[index] = (CharacterObject)null;
                            }
                        }
                    }
                }

            }
            else return;
        }
        public override void SyncData(IDataStore dataStore) 
        {
            if(SubModule.isGradual)
                dataStore.SyncData("SettlementCultureChanger", ref settlementTickCounters);
        }
        public void OnSettlementWeeklyTick(Settlement settlement)
        {
            if (settlementTickCounters.ContainsKey(settlement))
            {
                settlementTickCounters[settlement] += 1;
                if (IsSettlementReady(settlement))
                {
                    ChangeCulture(settlement);
                    settlementTickCounters.Remove(settlement);
                }
            }
        }
        public bool IsSettlementReady(Settlement settlement) => settlementTickCounters[settlement] == SubModule.weeksToPassForChange;
        public void AddSettlementCounter(Settlement settlement)
        {
            if (settlement.IsVillage || settlement.IsCastle || settlement.IsTown)
            {
                if (settlementTickCounters.ContainsKey(settlement))
                    settlementTickCounters[settlement] = 0;
                else
                    settlementTickCounters.Add(settlement, 0);
            }
        }
    }
}
