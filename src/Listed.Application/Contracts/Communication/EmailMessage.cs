namespace Listed.Application.Contracts.Communication;

public sealed record EmailMessage(
    string ToAddress,
    string Subject,
    string Body,
    bool IsBodyHtml = false);
