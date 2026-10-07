using System;
using Ashlight;
class BattleChecks {
 static int count;
 static void Check(bool ok) { count++; if(!ok) throw new Exception("Failed check " + count); }
 static void Main() {
  var b = new Battle();
  Check(b.Current == Phase.Player && b.HeroHealth == 100);
  Check(!b.Defend(false, 0));
  Check(b.Attack() && b.EnemyHealth == 75);
  Check(!b.Attack() && !b.Defend(false, 0));
  Check(b.BeginStrike());
  Check(!b.Defend(true, .19f));
  Check(b.Defend(false, .4f));
  Check(!b.Defend(true, .1f));
  b.FinishStrike(); Check(b.HeroHealth == 100 && b.Current == Phase.Player);
  b.Reset();
  for(int i=0;i<3;i++) { Check(b.Attack()); b.BeginStrike(); b.FinishStrike(); }
  Check(b.Current == Phase.Lost && b.HeroHealth == 0 && !b.Attack());
  b.Reset();
  for(int i=0;i<3;i++) { b.Attack(); if(b.Current == Phase.Won) break; b.BeginStrike(); Check(b.Defend(true,.18f)); b.FinishStrike(); }
  Check(b.Current == Phase.Won && b.EnemyHealth == 0 && b.HeroHealth == 100);
  b.FinishStrike(); Check(b.Current == Phase.Won);
  b.Reset(); Check(b.Current == Phase.Player && b.EnemyHealth == 100 && !b.Defended);
  b.Attack(); b.BeginStrike(); Check(!b.Defend(false,-.01f) && !b.Defend(false,.41f));
  Console.WriteLine("PASS: " + count + " combat checks (turn gating, damage, timing windows, counter, victory, defeat, reset).");
 }
}
