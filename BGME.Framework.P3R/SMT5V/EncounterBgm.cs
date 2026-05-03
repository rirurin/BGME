using BGME.Framework.Models;
using BGME.Framework.Music;
using BGME.Framework.P3R.SMT5V.Types;

namespace BGME.Framework.P3R.SMT5V;

internal unsafe class EncounterBgm : BaseEncounterBgm
{
    private delegate void SetNextStep(ABattleMainWorkBase* self, E_BTL_STEP step);
    private readonly SHFunction<SetNextStep>? _SetNextStepHook;

    public EncounterBgm(MusicService music)
        : base(music)
    {
        _SetNextStepHook = new SHFunction<SetNextStep>(SetNextStepImpl, "88 91 ?? ?? ?? ?? C6 81 ?? ?? ?? ?? 01");
    }

    public int BattleCueId { get; set; } = -1;

    public int VictoryCueId { get; set; } = -1;

    private void SetNextStepImpl(ABattleMainWorkBase* self, E_BTL_STEP step)
    {
        if (step == E_BTL_STEP.E_BTL_STEP_PRE)
        {
            this.BattleCueId = this.GetBattleMusic(self->m_DescData.m_EncID, GetEncounterContext(self->m_DescData.m_SymbolEncountType));
            this.VictoryCueId = this.GetVictoryMusic();
        }
        else if (step > E_BTL_STEP.E_BTL_STEP_RESULT)
        {
            // Only need to reset battle cue since
            // victory will always reset in above step.
            this.BattleCueId = -1;
        }

        Log.Debug($"{nameof(SetNextStepImpl)} || Step: {step}");
        _SetNextStepHook!.Hook.OriginalFunction(self, step);
    }

    private static EncounterContext GetEncounterContext(E_BTL_SYMBOL_ENCOUNT symbol)
        => symbol switch
        {
            E_BTL_SYMBOL_ENCOUNT.E_BTL_SYMBOL_ENCOUNT_PLAYER_ATTACK => EncounterContext.Advantage,
            E_BTL_SYMBOL_ENCOUNT.E_BTL_SYMBOL_ENCOUNT_ENEMY_BACK => EncounterContext.Disadvantage,
            _ => EncounterContext.Normal,
        };
}
