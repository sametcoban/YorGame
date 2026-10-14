using System;
using System.Collections.Generic;
namespace Ashlight {
    public enum InventoryView { All, Available, Equipped, Locked }
    public sealed class InventoryEntry {
        public readonly GearDefinition Gear;
        public readonly int Owned,Available,Equipped;
        public readonly bool Locked;
        internal InventoryEntry(GearDefinition gear,int owned,int available,bool locked) { Gear=gear; Owned=owned; Available=available; Equipped=owned-available; Locked=locked; }
    }
    [Serializable]
    public sealed class HeroBuildData {
        public string heroId,name;
        public int slot,firstSkill,secondSkill;
        public string[] gear;
    }
    public sealed partial class Battle {
        readonly Dictionary<string,HeroBuildData> heroBuilds=new Dictionary<string,HeroBuildData>();
        public List<InventoryEntry> InventoryItems(GearSlot? slot=null,HeroQuality? quality=null,Element? element=null,InventoryView view=InventoryView.All) {
            var rows=new List<InventoryEntry>();
            foreach(var item in GearDefinition.Catalog) {
                int owned=GearCount(item.Id); if(owned==0 || slot.HasValue && item.Slot!=slot.Value || quality.HasValue && item.Quality!=quality.Value || element.HasValue && item.Element!=element.Value) continue;
                int free=AvailableGear(item.Id); bool locked=GearLocked(item.Id);
                if(view==InventoryView.Available && free==0 || view==InventoryView.Equipped && free==owned || view==InventoryView.Locked && !locked) continue;
                rows.Add(new InventoryEntry(EnhancedGear(item.Id),owned,free,locked));
            }
            rows.Sort((a,b)=> { int order=b.Gear.Quality.CompareTo(a.Gear.Quality); return order!=0?order:string.CompareOrdinal(a.Gear.Id,b.Gear.Id); }); return rows;
        }
        public int RecycleValue(string id) { var item=GearDefinition.Find(id); return item==null || item.RelicChapter>=0?0:new[]{5,10,20,40,80}[(int)item.Quality]; }
        public string RecycleBlock(string id,int quantity) {
            if(!campaign || IsDailyBoss || (!CanChangeParty && !CampaignComplete)) return "Recycle gear from Home between battles.";
            var item=GearDefinition.Find(id); if(item==null || GearCount(item.Id)==0) return "Select owned gear.";
            if(item.RelicChapter>=0) return "Boss relics are protected; they cannot be earned again.";
            if(GearLocked(id)) return "This gear type is locked.";
            if(EnhancementLevel(id)>0) return "Upgraded gear types are protected.";
            if(quantity<1 || quantity>AvailableGear(id)) return "Only unequipped copies can be recycled.";
            if((long)quantity*RecycleValue(id)>1000000-ForgeMaterials) return "Not enough material capacity; choose fewer copies.";
            return "";
        }
        public bool RecycleGear(string id,int quantity) {
            if(RecycleBlock(id,quantity)!="") return false;
            int remaining=GearCount(id)-quantity;
            if(remaining==0) inventory.Remove(id); else inventory[id]=remaining;
            ForgeMaterials+=quantity*RecycleValue(id); return true;
        }
        static string BuildKey(string id,int slot) { return id+"|"+slot; }
        static HeroBuildData CopyBuild(HeroBuildData data) { if(data==null) return null; var gear=(string[])data.gear.Clone(); for(int i=0;i<gear.Length;i++) if(string.IsNullOrEmpty(gear[i])) gear[i]=null; return new HeroBuildData{heroId=data.heroId,name=data.name,slot=data.slot,firstSkill=data.firstSkill,secondSkill=data.secondSkill,gear=gear}; }
        public HeroBuildData HeroBuild(string id,int slot) { HeroBuildData data; return id==null || slot<0 || slot>=3 || !heroBuilds.TryGetValue(BuildKey(id,slot),out data)?null:CopyBuild(data); }
        public bool SaveHeroBuild(string id,int slot,string name) {
            if(!campaign || IsDailyBoss || (!CanChangeParty && !CampaignComplete) || slot<0 || slot>=3 || !ValidPresetName(name)) return false;
            var hero=id==null?null:HeroProgress(id); if(hero==null) return false;
            var gear=new string[3]; for(int i=0;i<3;i++) { var item=hero.Gear((GearSlot)i); gear[i]=item==null?null:item.Id; }
            heroBuilds[BuildKey(id,slot)]=new HeroBuildData{heroId=id,slot=slot,name=string.IsNullOrEmpty(name.Trim())?"Build "+(slot+1):name.Trim(),firstSkill=hero.SkillIndex(0),secondSkill=hero.SkillIndex(1),gear=gear}; return true;
        }
        public bool ClearHeroBuild(string id,int slot) { return campaign && !IsDailyBoss && (CanChangeParty || CampaignComplete) && id!=null && slot>=0 && slot<3 && heroBuilds.Remove(BuildKey(id,slot)); }
        public string HeroBuildBlock(string id,int slot) {
            if(!campaign || IsDailyBoss && !IsTraining || (!CanChangeParty && !CampaignComplete)) return "Load builds before fighting.";
            var hero=id==null?null:HeroProgress(id); var build=HeroBuild(id,slot); if(hero==null || build==null) return "This build slot is empty.";
            for(int i=0;i<3;i++) if(build.gear[i]!=null) {
                var current=hero.Gear((GearSlot)i); int ownCopy=current!=null && current.Id==build.gear[i]?1:0;
                if(AvailableGear(build.gear[i])+ownCopy<1) return GearDefinition.Find(build.gear[i]).Name+" is unavailable. Unequip it from another hero or earn another copy.";
            }
            return "";
        }
        public bool LoadHeroBuild(string id,int slot) {
            if(HeroBuildBlock(id,slot)!="") return false;
            var hero=HeroProgress(id); var build=HeroBuild(id,slot);
            // Check the entire build before changing any slot; never take another hero's gear.
            for(int i=0;i<3;i++) { var item=GearDefinition.Find(build.gear[i]); hero.SetGear((GearSlot)i,item); hero.SetGearEnhancement((GearSlot)i,item==null?0:EnhancementLevel(item.Id)); hero.SetRefinement((GearSlot)i,item==null?0:RelicRefinement(item.Id)); }
            hero.Equip(0,build.firstSkill); hero.Equip(1,build.secondSkill); hero.Restore(); return true;
        }
        HeroBuildData[] ExportHeroBuilds() { var rows=new List<HeroBuildData>(); foreach(var item in heroBuilds.Values) rows.Add(CopyBuild(item)); rows.Sort((a,b)=> { int order=string.CompareOrdinal(a.heroId,b.heroId); return order!=0?order:a.slot.CompareTo(b.slot); }); return rows.ToArray(); }
        static bool ValidateHeroBuilds(HeroBuildData[] builds,Dictionary<string,PartyHero> heroes) {
            if(builds==null || builds.Length>heroes.Count*3) return false;
            var keys=new HashSet<string>();
            foreach(var build in builds) {
                if(build==null || build.heroId==null || !heroes.ContainsKey(build.heroId) || build.slot<0 || build.slot>=3 || !keys.Add(BuildKey(build.heroId,build.slot)) || !ValidPresetName(build.name) || build.firstSkill<0 || build.firstSkill>=6 || build.secondSkill<0 || build.secondSkill>=6 || build.firstSkill==build.secondSkill || build.gear==null || build.gear.Length!=3) return false;
                for(int slot=0;slot<3;slot++) if(!string.IsNullOrEmpty(build.gear[slot])) { var item=GearDefinition.Find(build.gear[slot]); if(item==null || (int)item.Slot!=slot) return false; }
                // Templates reference known gear even if its last copy has been recycled.
            }
            return true;
        }
    }
}
