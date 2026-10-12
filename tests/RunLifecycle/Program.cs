using AutoDailyTribes.Core;
using AutoDailyTribes.Core.External;
using AutoDailyTribes.Core.Game;
using AutoDailyTribes.Core.Tasks;
using AutoDailyTribes.Core.Tribes;
using clib.Services;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

var tests = new (string Name, Action Body)[]
{
    ("empty queue does not borrow settings", () =>
    {
        new AutoTribeController().RunAll([]);
        Check(QuestionableSettings.BorrowCalls == 0);
    }),
    ("missing dependencies do not borrow settings", () =>
    {
        ExternalPlugins.Ready = false;
        new AutoTribeController().RunAll(Tribes());
        Check(QuestionableSettings.BorrowCalls == 0 && AutoTribe.Created == 0);
    }),
    ("settings exception is contained and restored", () =>
    {
        QuestionableSettings.ThrowOnBorrow = true;
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        CheckStopped(controller);
        Check(RunLog.Errors.Count == 1);
    }),
    ("first constructor failure is contained", () =>
    {
        AutoTribe.ThrowForTribe = "First";
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        CheckStopped(controller);
        Check(controller.Progress.Log.Single().Outcome == RunOutcome.Stopped);
        Check(RunLog.Errors.Single().Message == "constructor failed");
    }),
    ("later constructor failure stops the batch", () =>
    {
        AutoTribe.ThrowForTribe = "Second";
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        Svc.Automation.Complete();
        CheckStopped(controller);
        Check(controller.Progress.Log.Last().Name == "Second");
    }),
    ("start failure cancels constructed task", () =>
    {
        Svc.Automation.ThrowOnStart = true;
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        CheckStopped(controller);
        Check(Svc.Automation.LastTask!.Cancelled);
    }),
    ("execution fault does not start another tribe or run commands", () =>
    {
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        Svc.Automation.Complete(new Exception("execution failed"));
        CheckStopped(controller);
        Check(AutoTribe.Created == 1 && controller.Progress.Log.Count == 1);
    }),
    ("successful batch restores settings and dispatches once", () =>
    {
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        Svc.Automation.Complete();
        Check(controller.Running && QuestionableSettings.Borrowed && controller.Progress.Completed == 1);
        Svc.Automation.Complete();
        Check(!controller.Running && !QuestionableSettings.Borrowed && controller.Progress.Total == 0);
        Check(ChatCommands.Calls == 1);
    }),
    ("duplicate Start leaves active run intact", () =>
    {
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        var active = Svc.Automation.CurrentTask;
        controller.RunAll(Tribes());
        Check(ReferenceEquals(active, Svc.Automation.CurrentTask) && AutoTribe.Created == 1);
        Check(QuestionableSettings.BorrowCalls == 1);
    }),
    ("stuck last tribe suppresses commands and reports its unfinished quest", () =>
    {
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        Svc.Automation.Complete();
        Svc.Automation.Complete(outcome: RunOutcome.Partial, detail: "2 done, 1 stuck; remaining: Gulal Generosity");
        CheckStopped(controller);
        Check(controller.Progress.Log.Count == 2);
        Check(RunLog.Warnings.Single().Contains("Gulal Generosity"));
        var message = ECommons.DalamudServices.Svc.Chat.Messages.Single();
        Check(message.Contains("Second") && message.Contains("Gulal Generosity") && message.Contains("Post-run commands were skipped"));
    }),
    ("successful last tribe cannot hide an earlier partial result", () =>
    {
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        Svc.Automation.Complete(outcome: RunOutcome.Partial, detail: "1 fisher-only");
        Check(controller.Running && QuestionableSettings.Borrowed);
        Svc.Automation.Complete();
        CheckStopped(controller);
        Check(RunLog.Warnings.Single().Contains("First: 1 fisher-only"));
    }),
    ("skipped tribe suppresses commands", () =>
    {
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        Svc.Automation.Complete(outcome: RunOutcome.Skipped, detail: "no usable gearset");
        Svc.Automation.Complete();
        CheckStopped(controller);
        Check(RunLog.Warnings.Single().Contains("no usable gearset"));
    }),
    ("missing outcome never counts as success", () =>
    {
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        Svc.Automation.Complete(recordOutcome: false);
        Svc.Automation.Complete();
        CheckStopped(controller);
        Check(RunLog.Warnings.Single().Contains("Not every tribe reported"));
    }),
    ("a new successful run is not blocked by an old partial result", () =>
    {
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        Svc.Automation.Complete(outcome: RunOutcome.Partial);
        Svc.Automation.Complete();
        CheckStopped(controller);
        controller.RunAll(Tribes());
        Svc.Automation.Complete();
        Svc.Automation.Complete();
        Check(!controller.Running && !QuestionableSettings.Borrowed);
        Check(ChatCommands.Calls == 1);
        Check(controller.Progress.Log.All(entry => entry.Outcome == RunOutcome.Completed));
    }),
    ("old completion after Stop cannot advance a new run", () =>
    {
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        var oldCompletion = Svc.Automation.Completion!;
        controller.Stop();
        CheckStopped(controller);
        controller.RunAll(Tribes());
        var active = Svc.Automation.CurrentTask;
        oldCompletion();
        Check(ReferenceEquals(active, Svc.Automation.CurrentTask));
        Check(controller.Progress.Completed == 0 && QuestionableSettings.Borrowed);
    }),
    ("cleanup exception still restores settings and progress", () =>
    {
        var controller = new AutoTribeController();
        controller.RunAll(Tribes());
        Svc.Automation.ThrowOnStop = true;
        controller.Stop();
        Check(!QuestionableSettings.Borrowed && controller.Progress.Total == 0);
        Check(RunLog.Errors.Single().Message == "stop failed");
    }),
};

foreach (var (name, body) in tests)
{
    Reset();
    body();
    Console.WriteLine($"PASS {name}");
}

// Inspect the actual plugin binary without loading Dalamud/native game code.
if (args.Length != 1) throw new ArgumentException("Pass the built AutoDailyTribes.dll path to check hook-free inheritance.");
using var stream = File.OpenRead(args[0]);
using var pe = new PEReader(stream);
var metadata = pe.GetMetadataReader();
foreach (var name in new[] { "AutoCommon", "MoveOp" })
{
    var definition = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition)
        .Single(t => metadata.GetString(t.Namespace) == "AutoDailyTribes.Core.Tasks" && metadata.GetString(t.Name) == name);
    var parent = metadata.GetTypeReference((TypeReferenceHandle)definition.BaseType);
    Check(metadata.GetString(parent.Namespace) == "clib.Services" && metadata.GetString(parent.Name) == "AutoTask");
    Console.WriteLine($"PASS {name} inherits the hook-free AutoTask in the built plugin");
}
Console.WriteLine($"{tests.Length + 2} checks passed.");

static TribeInfo[] Tribes() => [new("First"), new("Second")];
static void Check(bool condition)
{
    if (!condition) throw new Exception("Regression check failed");
}
static void CheckStopped(AutoTribeController controller)
{
    Check(!controller.Running && !QuestionableSettings.Borrowed);
    Check(controller.Progress.Total == 0 && controller.Progress.Phase == TribePhase.Idle);
    Check(ChatCommands.Calls == 0);
}
static void Reset()
{
    Svc.Automation = new();
    AutoTribe.ThrowForTribe = null;
    AutoTribe.Created = 0;
    ExternalPlugins.Ready = true;
    QuestionableSettings.Borrowed = false;
    QuestionableSettings.ThrowOnBorrow = false;
    QuestionableSettings.BorrowCalls = 0;
    ChatCommands.Calls = 0;
    RunLog.Errors.Clear();
    RunLog.Warnings.Clear();
    ECommons.DalamudServices.Svc.Chat.Messages.Clear();
    ECommons.DalamudServices.Svc.Chat.Errors = 0;
}
