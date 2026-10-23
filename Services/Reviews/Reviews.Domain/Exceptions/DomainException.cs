using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reviews.Domain.Exceptions;

/// <summary>Порушення бізнес-правила домену → HTTP 400.</summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}