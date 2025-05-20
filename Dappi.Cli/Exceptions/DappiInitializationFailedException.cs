using System;

namespace Dappi.Cli.Exceptions;

public class DappiInitializationFailedException(string message) : Exception(message);