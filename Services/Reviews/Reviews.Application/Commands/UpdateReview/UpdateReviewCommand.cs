using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Reviews.Application.Common.Interfaces;

namespace Reviews.Application.Commands.UpdateReview;

/// <param name="ExpectedVersion">Версія, яку клієнт бачив (з ETag / If-Match).</param>
public record UpdateReviewCommand(
    string Id,
    int Rating,
    string Text,
    int ExpectedVersion) : ICommand;