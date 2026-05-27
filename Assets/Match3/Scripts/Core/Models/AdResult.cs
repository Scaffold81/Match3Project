#nullable enable

public enum AdFailReason { None, NoFill, NetworkError, Timeout, Unknown }

public readonly struct AdResult
{
    public bool         IsRewarded { get; }
    public AdFailReason FailReason { get; }

    private AdResult(bool isRewarded, AdFailReason failReason)
    {
        IsRewarded = isRewarded;
        FailReason = failReason;
    }

    public static AdResult Success()                    => new(true,  AdFailReason.None);
    public static AdResult Skip()                       => new(false, AdFailReason.None);
    public static AdResult Fail(AdFailReason reason)    => new(false, reason);
}
