using System;

namespace Tiwintza.Infrastructure.Common;

public sealed class DuplicateCodeException : Exception
{
    public DuplicateCodeException(string message) : base(message) { }
}
