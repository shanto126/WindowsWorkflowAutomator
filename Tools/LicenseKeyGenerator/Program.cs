using WindowsWorkflowAutomator.Licensing;

var plan = "PRO";
var expiry = DateTimeOffset.UtcNow.AddYears(1);

if (args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
{
    plan = args[0].Trim();
}

if (args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]))
{
    if (!DateTimeOffset.TryParse(args[1], out var parsedExpiry))
    {
        Console.Error.WriteLine("Expiry must be a valid DateTimeOffset value.");
        return 1;
    }

    expiry = parsedExpiry;
}

Console.WriteLine(LicenseKeyGenerator.Generate(plan, expiry));
return 0;
