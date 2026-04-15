using BGME.Framework.Music;
using BGME.Framework.P3R.SMT5V.Types;
using Ryo.Definitions.Structs;
using Ryo.Interfaces;
using System.Runtime.InteropServices;
using USoundAtomCue = BGME.Framework.P3R.SMT5V.Types.USoundAtomCue;
using USoundAtomCueSheet = BGME.Framework.P3R.SMT5V.Types.USoundAtomCueSheet;

namespace BGME.Framework.P3R.SMT5V;

internal unsafe class BgmService : BaseBgm
{
    private delegate void SetBGM(UProjectSoundManager* self, EBgmScene scene, USoundAtomCue* cue, EFadeType fade, byte bForceReplay);
    private readonly SHFunction<SetBGM> _SetBGM;

    private delegate void SetBGMScene(UProjectSoundManager* self, EBgmScene scene, EFadeType fade, byte bForceReplay);
    private readonly SHFunction<SetBGMScene> _SetBGMScene;

    private delegate USoundAtomCue* GetAtomCueById(USoundAtomCueSheet* cueSheet, int cueId);
    private readonly SHFunction<GetAtomCueById> _GetAtomCueById;

    private readonly ICriAtomEx _criAtomEx;
    private readonly EncounterBgm _encounterBgm;
    private bool _useVolumeFix;

    public BgmService(ICriAtomEx criAtomEx, MusicService music, EncounterBgm encounterBgm)
        : base(music)
    {
        _criAtomEx = criAtomEx;
        _encounterBgm = encounterBgm;

        _SetBGM = new SHFunction<SetBGM>(SetBGMImpl, "40 53 57 41 56 41 57 48 83 EC 48 80 B9 ?? ?? ?? ?? 00");
        _SetBGMScene = new SHFunction<SetBGMScene>(SetBGMSceneImpl, "48 89 5C 24 ?? 48 89 74 24 ?? 55 57 41 54 41 56 41 57 48 8B EC 48 83 EC 50 0F B6 B9");
        _GetAtomCueById = new SHFunction<GetAtomCueById>("40 53 55 48 81 EC 28 02 00 00 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 8B DA 48 8B E9 E8 ?? ?? ?? ?? 48");
    }

    public void SetVolumeFix(bool value) => _useVolumeFix = value;

    protected override int VictoryBgmId { get; } = 199;

    private void SetBGMSceneImpl(UProjectSoundManager* self, EBgmScene scene, EFadeType fade, byte bForceReplay)
    {
        Log.Debug($"{nameof(SetBGMScene)} || {scene} || {fade}");
        _SetBGMScene!.Hook.OriginalFunction(self, scene, fade, bForceReplay);

        if (_useVolumeFix && fade != EFadeType.PlayAfterFadeout && fade != EFadeType.NoFade)
        {
            self->BGMRequest.FadeTime = 1.0f;
            self->BGMRequest.FadeType = EFadeType.PlayAfterFadeout;
            Log.Verbose($"{nameof(SetBGM)} || (VolumeFix) Replaced {fade} with {EFadeType.PlayAfterFadeout} / 1s");
        }
    }

    private void SetBGMImpl(UProjectSoundManager* self, EBgmScene scene, USoundAtomCue* cue, EFadeType fade, byte bForceReplay)
    {
        if (cue == null)
        {
            _SetBGM!.Hook.OriginalFunction(self, scene, cue, fade, bForceReplay);
            return;
        }

        Log.Debug($"{nameof(SetBGM)} || Scene: {scene} || Fade: {fade}");

        var ogCueId = GetSoundCueId(cue);
        if (scene == EBgmScene.Battle)
        {
            ogCueId = _encounterBgm.BattleCueId;
        }
        else if (scene == EBgmScene.Result)
        {
            ogCueId = _encounterBgm.VictoryCueId;
        }

        var currentCueId = this.GetGlobalBgmId(ogCueId);

        if (currentCueId == null)
        {
            cue = null;
            this.ClearCustomCue();
        }
        else if (currentCueId != -1)
        {
            cue = this.GetCustomCueById(cue->CueSheet, (int)currentCueId);
            Log.Debug($"Playing BGM ID: {currentCueId}");
        }
        else
        {
            this.ClearCustomCue();
        }

        _SetBGM!.Hook.OriginalFunction(self, scene, cue, fade, bForceReplay);
        if (_useVolumeFix && fade != EFadeType.PlayAfterFadeout && fade != EFadeType.NoFade)
        {
            self->BGMRequest.FadeTime = 1.0f;
            self->BGMRequest.FadeType = EFadeType.PlayAfterFadeout;
            Log.Verbose($"{nameof(SetBGM)} || (VolumeFix) Replaced {fade} with {EFadeType.PlayAfterFadeout} / 1s");
        }
    }

    private int GetSoundCueId(USoundAtomCue* cue)
    {
        var cueName = Marshal.StringToHGlobalAnsi(cue->CueName.GetString());
        var info = (CriAtomExCueInfoTag*)Marshal.AllocHGlobal(sizeof(CriAtomExCueInfoTag));
        _criAtomEx.Acb_GetCueInfoByName(cue->CueSheet->acbHn, cueName, info);

        var cueId = info->id;

        Marshal.FreeHGlobal((nint)info);
        Marshal.FreeHGlobal(cueName);
        return cueId;
    }

    private int _currCustomCueId = -1;
    private USoundAtomCue* _currentCustomCue = null;

    private USoundAtomCue* GetCustomCueById(USoundAtomCueSheet* cueSheet, int cueId)
    {
        if (_currCustomCueId == cueId && _currentCustomCue != null)
        {
            return _currentCustomCue;
        }

        _currentCustomCue = _GetAtomCueById.Hook.OriginalFunction(cueSheet, cueId);
        _currCustomCueId = cueId;
        return _currentCustomCue;
    }

    private void ClearCustomCue()
    {
        _currCustomCueId = -1;
        _currentCustomCue = null;
    }

    protected override void PlayBgm(int bgmId)
    {
        throw new NotImplementedException();
    }
}
