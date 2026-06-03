using Dalamud.Game.ClientState.Conditions;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.Game;
using System.Threading.Tasks;

namespace AutoDailyTribes.Core.Tasks;

public sealed partial class AutoTribe
{
    private const uint ReviveCommandId   = (uint)clib.Enums.CommandFlag.Revive;        // 200
    private const int  ReviveParamReturn = (int)clib.Enums.AgentReviveOp.Return;        // 8 — return to home point
    private const int  ReviveParamAccept = (int)clib.Enums.AgentReviveOp.AcceptRevive;  // 5 — accept a raise
    private const int  ReturnReissueMs   = 1_500;
    private const int  RaiseWaitMs = 30_000;
    private const int  ReleaseTransitionWaitMs = 60_000;

    private static bool IsPlayerKO() => Svc.Condition[ConditionFlag.Unconscious];

    private async Task Revive()
    {
        var soloWait = Svc.Party.Length == 0;
        Status = soloWait ? "KO — releasing" : "KO — waiting for raise";
        Diag(soloWait
            ? $"{tribe.Name}: solo KO; returning home."
            : $"{tribe.Name}: party KO; waiting up to {RaiseWaitMs / 1000}s for a raise.");

        var returningHome = soloWait;
        if (soloWait)
        {
            TriggerReturnHome();
        }
        else
        {
            var raiseDeadline = Environment.TickCount64 + RaiseWaitMs;
            var accepted = false;
            while (Environment.TickCount64 < raiseDeadline)
            {
                if (CancelToken.IsCancellationRequested) return;
                if (!IsPlayerKO()) { Diag($"{tribe.Name}: raised by another player."); accepted = true; break; }
                if (TryAcceptRaisePrompt()) { Diag($"{tribe.Name}: accepted raise prompt."); accepted = true; break; }
                await NextFrame(30);
            }

            if (!accepted)
            {
                Diag($"{tribe.Name}: no raise within window; falling back to return-home.");
                TriggerReturnHome();
                returningHome = true;
            }
        }

        await WaitForReviveOrTransition(reissueReturn: returningHome);

        if (IsPlayerKO())
        {
            Diag($"{tribe.Name}: still KO after revive window; outer loop will retry.");
            return;
        }

        arrivedAtIssuer = false;
    }

    private void TriggerReturnHome()
    {
        if (!TryExecuteReviveCommand(ReviveParamReturn))
            Diag($"{tribe.Name}: return-home command dispatch failed.");
    }

    private static bool TryExecuteReviveCommand(int param)
    {
        try
        {
            GameMain.ExecuteCommand((int)ReviveCommandId, param, 0, 0, 0);
            return true;
        }
        catch (Exception ex)
        {
            Svc.Log.Warning(ex, "[ADT] GameMain.ExecuteCommand revive failed");
            return false;
        }
    }

    private static bool TryAcceptRaisePrompt()
    {
        return TryExecuteReviveCommand(ReviveParamAccept);
    }

    private async Task WaitForReviveOrTransition(bool reissueReturn)
    {
        var deadline = Environment.TickCount64 + ReleaseTransitionWaitMs;
        var nextReissueAt = Environment.TickCount64 + ReturnReissueMs;
        while (Environment.TickCount64 < deadline)
        {
            if (CancelToken.IsCancellationRequested) return;
            var stillKO = IsPlayerKO();
            var transitioning = Svc.Condition[ConditionFlag.BetweenAreas]
                             || Svc.Condition[ConditionFlag.BetweenAreas51];
            if (!stillKO && !transitioning)
            {
                await NextFrame(60);
                return;
            }
            if (reissueReturn && stillKO && !transitioning && Environment.TickCount64 >= nextReissueAt)
            {
                TriggerReturnHome();
                nextReissueAt = Environment.TickCount64 + ReturnReissueMs;
            }
            await NextFrame(30);
        }
        Diag($"{tribe.Name}: revive transition timed out; outer loop will retry.");
    }
}
