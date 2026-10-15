namespace FairShare.PaymentSimulator;

/// <summary>Every error of the simulator: a stable code for programs and a message for people.</summary>
public sealed record ErrorResponse(string Error, string Message);
