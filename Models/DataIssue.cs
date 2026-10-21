namespace ZellburyQueueSim.Models;

public enum IssueSeverity
{
    Info,
    Warning,
    Error
}

/// <summary>
/// One finding from validating the recorded timestamps. <see cref="Row"/> is the 1-based
/// customer number in the sheet's row order (null for a finding about the whole counter).
/// <see cref="Code"/> is a stable machine-readable tag for the kind of finding (may be empty).
/// </summary>
public record DataIssue(IssueSeverity Severity, int? Row, string Message, string Code = "");

public static class DataIssueCodes
{
    public const string IdleWhileWaiting = "idle-while-waiting";
}
