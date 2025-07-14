using BGME.Framework.Music;
using Reloaded.Hooks.Definitions.X64;
using Timer = System.Timers.Timer;

namespace BGME.Framework.P5R;

internal unsafe class BgmService : BaseBgm
{
    [Function(CallingConventions.Microsoft)]
    private delegate void PlayBgmFunction(int cueId);
    private readonly SHFunction<PlayBgmFunction> playBgm;

    public BgmService(MusicService music)
        : base(music)
    {
        playBgm = new(PlayBgm, "40 53 48 83 EC 30 89 CB");
    }
    
    protected override int VictoryBgmId { get; } = 340;

    protected override void PlayBgm(int cueId)
    {
        var currentBgmId = this.GetGlobalBgmId(cueId);
        if (currentBgmId == null)
        {
            return;
        }

        Log.Debug($"Playing BGM ID: {currentBgmId}");
        this.playBgm.OriginalFunction((int)currentBgmId);
    }
}
