using System;

namespace Dappi.Cli.Exceptions;

public class DappiReleaseDoesNotExistException(string message, Exception? innerException = null)
    : Exception(message, innerException);