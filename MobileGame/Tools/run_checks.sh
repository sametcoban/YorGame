#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
suite="${1:-battle}"
if (( $# )); then shift; fi
case "$suite" in
  battle) fixture=BattleChecks;;
  balance) fixture=BalanceChecks;;
  combined) fixture=CombinedProgression;;
  mastery) fixture=MasteryBudget;;
  *) echo "Usage: $0 [battle|balance|combined|mastery] [fixture arguments]" >&2; exit 2;;
esac
command -v mcs >/dev/null || { echo 'Mono C# compiler is required; activate the cloud Mono tools or install Mono.' >&2; exit 1; }
command -v mono >/dev/null
build_dir=$(mktemp -d "${TMPDIR:-/tmp}/ashlight-validation.XXXXXX")
trap 'rm -rf "$build_dir"' EXIT
sources=(Assets/Scripts/Battle.cs Assets/Scripts/Battle.*.cs)
for name in BattleResults HeroSignature BossPresentation BetaSupport CharacterClass SkillDefinition HeroDefinition Elements ChapterDefinition EnemyVisualDefinition ProgressData Summoning CombatMotion AutoBattlePlanner EnemySkills UltimateDefinition GearDefinition ChapterStageDefinition SkillPresentation; do
  sources+=("Assets/Scripts/$name.cs")
done
mcs -debug -out:"$build_dir/checks.exe" "${sources[@]}" "Validation/$fixture.cs"
mono --debug "$build_dir/checks.exe" "$@"
