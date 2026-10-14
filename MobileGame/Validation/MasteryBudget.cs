using System;
using Ashlight;
class MasteryBudget {
 static void Main() {
  var clock=new DateTime(2026,10,14,0,0,0,DateTimeKind.Utc); var b=new Battle(true,utcNow:()=>clock); var save=b.ExportProgress(); save.encounter=60; save.loadouts[0].level=20; save.forgeMaterials=0;
  if(!b.RestoreProgress(save)) throw new Exception("Budget fixture rejected");
  int upgrades=0,total=0;
  for(int day=1;day<=7;day++) { clock=clock.AddHours(12); int earned=b.IdleForgeMaterials; if(!b.ClaimIdleRewards()) throw new Exception("Earned materials rejected"); total+=earned;
   while(b.CanMasterHero("Knight_Common")) { if(!b.MasterHero("Knight_Common")) throw new Exception("Mastery rejected"); upgrades++; }
   var loaded=new Battle(true,utcNow:()=>clock); if(!loaded.RestoreProgress(b.ExportProgress())) throw new Exception("Mastery reload rejected"); b=loaded;
  }
  if(total!=5040 || upgrades!=5 || b.ForgeMaterials!=40 || b.Party[0].CombatLevel!=30 || b.Party[0].Level!=20) throw new Exception("Earned mastery budget mismatch");
  Console.WriteLine("PASS: five mastery upgrades cost 5,000 earned materials; seven capped idle claims fund one hero, with 40 materials left. Forge spending and other rewards excluded.");
 }
}
