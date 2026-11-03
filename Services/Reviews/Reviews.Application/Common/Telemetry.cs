using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Reviews.Application.Common;

public static class Telemetry
{
    public static readonly ActivitySource Source = new("Reviews.WebApi");
}