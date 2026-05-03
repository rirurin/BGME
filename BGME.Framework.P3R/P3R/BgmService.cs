using BGME.Framework.Music;
using Reloaded.Hooks.Definitions;
using Reloaded.Hooks.Definitions.X64;
using Ryo.Definitions.Structs;
using Ryo.Interfaces;

namespace BGME.Framework.P3R.P3R;

internal unsafe class BgmService : BaseBgm
{
    [Function(CallingConventions.Microsoft)]
    private delegate void PlayBgmFunction(int bgmId);
    private PlayBgmFunction? playBgm;

    [Function(CallingConventions.Microsoft)]
    private delegate void RequestSound(UPlayAdxControl* self, int playerMajorId, int playerMinorId, int cueId, nint param5);

    private readonly ICriAtomEx criAtomEx;
    private IHook<RequestSound>? requestSoundHook;
    
    private static string[] RequestSoundCandidates =
    [
        "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 4C 89 74 24 ?? 45 31 D2",
        "48 89 5C 24 ?? 48 89 74 24 ?? 48 89 7C 24 ?? 4C 89 74 24 ?? 45 33 D2"
    ];
    private readonly object RequestSoundLock = new();
    private int RequestSoundSignaturesScanned;

    public BgmService(ICriAtomEx criAtomEx, MusicService music)
        : base(music)
    {
        this.criAtomEx = criAtomEx;
        
        foreach (var (Index, Candidate) in RequestSoundCandidates.Select((x, i) => (i, x)))
        {
            Project.Scans.AddScanHook($"RequestSound[{Index}]", Candidate, (result, hooks) =>
                {
                    lock (RequestSoundLock)
                    {
                        RequestSoundSignaturesScanned++;
                        this.requestSoundHook ??= hooks.CreateHook<RequestSound>(RequestSoundImpl, result).Activate();
                    }
                },
                () =>
                {
                    lock (RequestSoundLock)
                    {
                        RequestSoundSignaturesScanned++;
                        if (RequestSoundSignaturesScanned == RequestSoundCandidates.Length)
                        {
                            Log.Error($"Failed to find a pattern for RequestSound.");
                        }
                        else
                        {
                            Log.Debug($"No matching pattern for RequestSound[{Index}].");
                        }
                    }
                });
        }
    }

    protected override int VictoryBgmId { get; } = 60;

    private void RequestSoundImpl(UPlayAdxControl* self, int playerMajorId, int playerMinorId, int cueId, nint param5)
    {
        Log.Verbose($"{nameof(RequestSound)} || Player: {playerMajorId} / {playerMinorId} || Cue ID: {cueId} || param5: {param5}");
        if (playerMajorId != 0 || playerMinorId != 0)
        {
            this.requestSoundHook!.OriginalFunction(self, playerMajorId, playerMinorId, cueId, param5);
            return;
        }

        var currentBgmId = this.GetGlobalBgmId(cueId);
        if (currentBgmId == null)
        {
            return;
        }

        Log.Debug($"Playing BGM ID: {currentBgmId}");
        this.requestSoundHook!.OriginalFunction(self, playerMajorId, playerMinorId, (int)currentBgmId, param5);
    }

    protected override void PlayBgm(int bgmId)
        => this.playBgm!(bgmId);

    private static bool IsDlcBgm(int bgmId)
        => bgmId >= 1000 && bgmId <= 1100;
}
