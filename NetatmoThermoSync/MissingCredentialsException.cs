namespace NetatmoThermoSync;

public sealed class MissingCredentialsException()
    : Exception("Netatmo credentials not configured.");
