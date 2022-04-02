using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace SettlementCultureChanger
{
    public class ChangeSettlementCulture : CampaignBehaviorBase
    {
        static Dictionary<Settlement, CultureObject> initialCultureDictionary = new Dictionary<Settlement, CultureObject>();
        Dictionary<Settlement, int> settlementTickCounters = new Dictionary<Settlement, int>();
        public override void RegisterEvents()
        {
            if (SubModule.isGradual)
                CampaignEvents.WeeklyTickEvent.AddNonSerializedListener((object)this, new Action(this.OnSettlementWeeklyTick));
           
            CampaignEvents.OnSiegeAftermathAppliedEvent.AddNonSerializedListener((object)this,new Action<MobileParty, Settlement, SiegeAftermathCampaignBehavior.SiegeAftermath, Clan, Dictionary<MobileParty, float>>(this.OnSiegeAftermathApplied));
            CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener((object)this, new Action<Settlement, bool, Hero, Hero, Hero, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail>(this.OnSettlementOwnerChanged));
            CampaignEvents.ClanChangedKingdom.AddNonSerializedListener((object)this, new Action<Clan, Kingdom, Kingdom, ChangeKingdomAction.ChangeKingdomActionDetail, bool>(this.OnClanChangedKingdom));
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(this.OnGameLoaded));
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener((object)this, new Action<CampaignGameStarter>(this.OnGameLoaded));
        }
        
        private void OnGameLoaded(CampaignGameStarter obj)
        {
            Dictionary<Settlement, CultureObject> initialCultureList = new Dictionary<Settlement, CultureObject>();
            foreach (Settlement settlement in Campaign.Current.Settlements.Where(s => s.IsTown || s.IsCastle || s.IsVillage))
            {
                AddToInitialCultureList(settlement, settlement.Culture);
                if (!SubModule.isGradual)
                    ChangeCulture(settlement,true);
            }
            if (SubModule.isGradual)
            {
                foreach (Settlement settlement in Campaign.Current.Settlements.Where(s => s.IsTown || s.IsCastle || s.IsVillage))
                {
                    if (settlement.Culture != (settlement.OwnerClan.Kingdom?.Culture ?? settlement.OwnerClan.Culture) && !settlementTickCounters.ContainsKey(settlement))
                    {
                        AddSettlementCounter(settlement);
                    }
                    else if (settlement.Culture != (settlement.OwnerClan.Kingdom?.Culture ?? settlement.OwnerClan.Culture) &&
                             settlementTickCounters.ContainsKey(settlement) && IsSettlementReady(settlement))
                    {
                        ChangeCulture(settlement,false);
                    }
                }
            }
        }

        private void OnSiegeAftermathApplied(MobileParty arg1, Settlement settlement, SiegeAftermathCampaignBehavior.SiegeAftermath arg3, Clan arg4, Dictionary<MobileParty, float> arg5)
        {
            if (SubModule.isGradual)
                AddSettlementCounter(settlement);
            else
                ChangeCulture(settlement,true);
        }

        private void OnSettlementOwnerChanged(Settlement settlement, bool arg2, Hero arg3, Hero arg4, Hero arg5, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
        {
            if (detail != ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
            {
                if (SubModule.isGradual)
                    AddSettlementCounter(settlement);
                else
                    ChangeCulture(settlement,true);
            }
            else if (detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.BySiege)
            {
                RevertCulture(settlement);
            }
            
        }

        private void OnClanChangedKingdom(Clan clan, Kingdom arg2, Kingdom arg3, ChangeKingdomAction.ChangeKingdomActionDetail arg4, bool arg5)
        {
            OnKingdomChange(clan);
        }
        public static void SetInitialCultureList(Dictionary<Settlement, CultureObject> dict) => initialCultureDictionary = dict;
        public static void AddToInitialCultureList(Settlement settlement, CultureObject culture) => initialCultureDictionary.Add(settlement, culture);
        public static void ChangeCulture(Settlement settlement,bool deleteTroops)
        {
            if (!(settlement.IsVillage || settlement.IsCastle || settlement.IsTown)) { return; }

            if (settlement.Culture != (settlement.OwnerClan.Kingdom?.Culture ?? settlement.OwnerClan.Culture))
            {
                settlement.Culture = settlement.OwnerClan.Kingdom?.Culture ?? settlement.OwnerClan.Culture;
                ChangeNotableCulture(settlement);
                if(deleteTroops)
                    DeleteNotableTroops(settlement);
                foreach (Village boundVillage in settlement.BoundVillages)
                {
                    if(deleteTroops)
                        ChangeCulture(boundVillage.Settlement,true);
                    else
                        ChangeCulture(boundVillage.Settlement,false);
                }
            }
        }
        public static void ChangeNotableCulture(Settlement settlement)
        {
            if (!SubModule.lightMode)
            {
                foreach (Hero notable in settlement.Notables)
                {
                    notable.Culture = settlement.OwnerClan.Kingdom?.Culture ?? settlement.OwnerClan.Culture;
                }
            }
            else
            {
                foreach (Hero notable in settlement.Notables)
                {
                    if (settlement.IsVillage)
                    {
                        notable.Culture = initialCultureDictionary[settlement.Village.Bound];
                    }
                    else
                    {
                        notable.Culture = initialCultureDictionary[settlement];
                    }
                }
            }
        }
        public void OnKingdomChange(Clan clan)
        {
            if (clan.Culture == clan.Kingdom?.Culture) return;
            foreach (Settlement settlement in clan.Settlements.Where(s => s.IsTown || s.IsCastle || s.IsVillage))
            {
                if (SubModule.isGradual)
                    AddSettlementCounter(settlement);
                else
                    ChangeCulture(settlement,true);
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
        public void OnSettlementWeeklyTick()
        {
            List<Settlement> settlements = Campaign.Current.Settlements.Where(s => settlementTickCounters.ContainsKey(s)).ToList();
            settlements.ForEach(settlement =>
            {
                if (settlementTickCounters.ContainsKey(settlement))
                {
                    settlementTickCounters[settlement] += 1;
                    if (IsSettlementReady(settlement))
                    {
                        ChangeCulture(settlement, true);
                    }
                }
            });
            
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
